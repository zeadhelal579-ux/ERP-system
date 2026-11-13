using ERP_system.Common;
using ERP_system.Data;
using ERP_system.DTOs.Production;
using ERP_system.Exceptions;
using ERP_system.Models;
using ERP_system.Repositories;
using ERP_system.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ERP_system.Services
{
    public class ProductionService : IProductionService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;
        private readonly ILogger<ProductionService> _logger;
        private readonly AppDbContext _db;

        public ProductionService(IUnitOfWork unitOfWork, IAuditService auditService, ILogger<ProductionService> logger, AppDbContext db)
        {
            _unitOfWork = unitOfWork;
            _auditService = auditService;
            _logger = logger;
            _db = db;
        }

        public async Task<BatchResponseDto> StartBatchAsync(StartBatchDto request, int userId)
        {
            var previous = request.RequestId is Guid requestId
                ? await _db.ProductionBatches.Include(batch => batch.MaterialIssues)
                    .SingleOrDefaultAsync(batch => batch.StartRequestId == requestId)
                : null;
            if (previous is not null)
            {
                var requestedIssues = request.AdditionalIssueIds.Append(request.PrimaryIssueId ?? 0);
                if (previous.CreatedBy != userId || previous.ProductId != request.ProductId ||
                    previous.RawMaterialId != request.RawMaterialId || previous.IssuedMaterialQty != request.IssuedMaterialQty ||
                    !previous.MaterialIssues.Select(issue => issue.Id).Order().SequenceEqual(requestedIssues.Order()))
                    throw new ConflictException("معرّف الطلب مرتبط بتشغيلة مختلفة", "REQUEST_ID_CONFLICT");
                return MapToDto(previous);
            }

            var material = await _unitOfWork.RawMaterials.GetByIdAsync(request.RawMaterialId);

            if (material == null)
                throw NotFoundException.For("الخامة", "MATERIAL_NOT_FOUND");

            if (!material.IsQCApproved)
                throw new BusinessRuleException("MATERIAL_NOT_QC_APPROVED", "الخامة لسه ماوافقش عليها المعمل");

            // ملاحظة: مفيش خصم هنا خالص - الخصم الفعلي بيحصل في StoreService.IssueMaterialAsync
            // وقت ما المخزن يصرف الخامة فعلياً. هنا بس بنتأكد إن الرصيد كان كافي وقت الصرف.
            if (material.CurrentQuantity < 0)
                throw new BusinessRuleException("INVALID_MATERIAL_STATE", "حالة رصيد الخامة غير طبيعية"); // حماية إضافية بس مش المفروض تحصل أبداً

            if (request.PrimaryIssueId is null)
                throw new BusinessRuleException("MATERIAL_ISSUE_REQUIRED", "اختار حركة صرف أساسية قبل بدء التشغيلة");

            var issueIds = request.AdditionalIssueIds.Prepend(request.PrimaryIssueId.Value).ToArray();
            var issues = await _db.MaterialIssues.Include(issue => issue.Material)
                .Where(issue => issueIds.Contains(issue.Id)).ToListAsync();
            if (issues.Count != issueIds.Length ||
                issues.Any(issue => issue.BatchId != null || !issue.Material.IsQCApproved ||
                    issue.Material.ExpiryDate.Date < DateTime.UtcNow.Date))
                throw new ConflictException("في حركة صرف مستخدمة أو دفعة غير متاحة", "ISSUE_UNAVAILABLE");

            var primaryIssue = issues.Single(issue => issue.Id == request.PrimaryIssueId);
            if (primaryIssue.RawMaterialId != material.Id || primaryIssue.Quantity != request.IssuedMaterialQty)
                throw new BusinessRuleException("ISSUE_MISMATCH", "الخامة والكمية لازم يطابقوا حركة الصرف الأساسية");

            foreach (var issueMaterial in issues.Select(issue => issue.Material).Distinct())
                _db.Entry(issueMaterial).Property(item => item.IsQCApproved).IsModified = true;

            var product = await _unitOfWork.Products.GetByIdAsync(request.ProductId);
            if (product == null)
                throw NotFoundException.For("المنتج", "PRODUCT_NOT_FOUND");

            var batch = new ProductionBatch
            {
                BatchNumber = GenerateBatchNumber(),
                StartRequestId = request.RequestId,
                MaterialIssues = issues,
                ProductId = request.ProductId,
                RawMaterialId = request.RawMaterialId,
                IssuedMaterialQty = request.IssuedMaterialQty,
                ProducedQty = 0,
                ReturnedMaterialQty = 0,
                ScrapQty = 0,
                CurrentState = "In_Production",
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.ProductionBatches.AddAsync(batch);

            // Finding #4: نفس إصلاح StoreService — الـ Audit بيستخدم BatchNumber (متولّد
            // محليًا قبل الحفظ) مش الـ Id، فمفيش داعي لحفظتين منفصلتين.
            await _auditService.LogAsync(userId, "Batch_Started", "Batch", batch.BatchNumber,
                $"خامة مستخدمة: {material.MaterialName}, كمية: {request.IssuedMaterialQty}");

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("تشغيلة جديدة {BatchNumber} بدأت للمنتج {ProductId}", batch.BatchNumber, batch.ProductId);

            return MapToDto(batch);
        }

        public async Task<BatchResponseDto> FinishBatchAsync(FinishBatchDto request, int userId)
        {
            var batch = await _unitOfWork.ProductionBatches.GetByIdAsync(request.BatchId);

            if (batch == null)
                throw NotFoundException.For("التشغيلة", "BATCH_NOT_FOUND");

            if (batch.IsHeld)
                throw new ConflictException("التشغيلة محجوزة من الجودة", "BATCH_HELD");

            if (batch.CurrentState != "In_Production" && batch.CurrentState != "Rework_In_Progress")
                throw new InvalidStateTransitionException(
                    $"لا يمكن إنهاء التشغيلة وهي في حالة '{batch.CurrentState}'");

            // الشرط الأهم: مفيش تلاعب - مجموع السليم والمرتجع لازم يكون منطقي
            var totalAccounted = request.ProducedQty + request.ReturnedMaterialQty;
            if (totalAccounted > batch.IssuedMaterialQty)
                throw new BusinessRuleException("QUANTITY_MISMATCH",
                    "مستحيل يطلع سليم + مرتجع أكتر من اللي اتصرف أصلاً");

            // === المعادلة الذهبية: الهالك بيتحسب هنا وبس ===
            var scrap = batch.IssuedMaterialQty - totalAccounted;

            batch.ProducedQty = request.ProducedQty;
            batch.ReturnedMaterialQty = request.ReturnedMaterialQty;
            batch.ScrapQty = scrap;
            batch.CurrentState = "Waiting_QC";
            batch.ExpiryDate = request.ExpiryDate;

            _unitOfWork.ProductionBatches.Update(batch);

            // رجّع الفائض السليم لرصيد المخزن
            if (request.ReturnedMaterialQty > 0)
            {
                var material = await _unitOfWork.RawMaterials.GetByIdAsync(batch.RawMaterialId);
                if (material != null)
                {
                    material.CurrentQuantity += request.ReturnedMaterialQty;
                    _unitOfWork.RawMaterials.Update(material);
                }
            }

            // ==================================================================
            // Finding #8: هالك خط الإنتاج كان بيتسجّل كرقم على التشغيلة بس (ScrapQty)
            // من غير ما يظهر كسطر في الجدول المركزي للهالك — يخالف نص المتطلبات
            // "جدول مركزي لتسجيل أي هالك من أي قسم". دلوقتي أي هالك محسوب هنا (> 0)
            // بينشئ سطر ScrapAndQuarantine تلقائيًا، بنفس منطق هالك المخزن تمامًا
            // (Destroyed مباشرة، لأنه هالك فعلي حصل بالفعل ومفيش قرار تاني يُتّخذ عليه).
            // ==================================================================
            if (scrap > 0)
            {
                var productionScrapEntry = new ScrapAndQuarantine
                {
                    SourceType = "ProductionLine",
                    ReferenceId = batch.Id,
                    Quantity = scrap,
                    ScrapReason = "هالك خط إنتاج محسوب تلقائيًا أثناء إنهاء التشغيلة",
                    FinalDecision = "Destroyed"
                };

                await _unitOfWork.ScrapAndQuarantines.AddAsync(productionScrapEntry);
            }

            await _auditService.LogAsync(userId, "Batch_Finished", "Batch", batch.BatchNumber,
                $"منتَج: {request.ProducedQty}, مرتجع: {request.ReturnedMaterialQty}, هالك محسوب: {scrap}");

            try
            {
                await _unitOfWork.SaveChangesAsync(); // كل التعديلات دي بتتحفظ سوا في نفس اللحظة
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex, "تعارض تزامن أثناء إنهاء التشغيلة {BatchNumber}", batch.BatchNumber);
                throw new ConflictException(
                    "التشغيلة اتعدّلت من عملية تانية في نفس اللحظة، من فضلك أعد المحاولة",
                    "CONCURRENCY_CONFLICT");
            }

            _logger.LogInformation("تشغيلة {BatchNumber} انتهت، هالك محسوب: {Scrap}", batch.BatchNumber, scrap);

            return MapToDto(batch);
        }

        public async Task<PagedResult<BatchResponseDto>> GetAllBatchesAsync(PaginationParams pagination)
        {
            var query = _unitOfWork.ProductionBatches.Query();

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderByDescending(b => b.Id)
                .Skip(pagination.Skip)
                .Take(pagination.PageSize)
                .ToListAsync();

            return new PagedResult<BatchResponseDto>
            {
                Items = items.Select(MapToDto),
                Page = pagination.Page,
                PageSize = pagination.PageSize,
                TotalCount = totalCount
            };
        }

        public async Task<BatchResponseDto> GetBatchByIdAsync(int id)
        {
            var batch = await _unitOfWork.ProductionBatches.GetByIdAsync(id);
            if (batch == null)
                throw NotFoundException.For("التشغيلة", "BATCH_NOT_FOUND");

            return MapToDto(batch);
        }

        private static string GenerateBatchNumber()
        {
            // Finding #12: توحيد كل توقيتات النظام على UTC
            return $"BATCH-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 6).ToUpper()}";
        }

        private static BatchResponseDto MapToDto(ProductionBatch b) => BatchMapper.ToDto(b);
    }
}
