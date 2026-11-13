using ERP_system.Common;
using ERP_system.DTOs.Store;
using ERP_system.Data;
using ERP_system.Exceptions;
using ERP_system.Models;
using ERP_system.Repositories;
using ERP_system.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ERP_system.Services
{
    public class StoreService : IStoreService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;
        private readonly ILogger<StoreService> _logger;
        private readonly AppDbContext _db;

        public StoreService(IUnitOfWork unitOfWork, IAuditService auditService, ILogger<StoreService> logger, AppDbContext db)
        {
            _unitOfWork = unitOfWork;
            _auditService = auditService;
            _logger = logger;
            _db = db;
        }

        public async Task<RawMaterialResponseDto> AddRawMaterialAsync(AddRawMaterialDto request, int userId)
        {
            var material = new RawMaterial
            {
                LotCode = $"LOT-{Guid.NewGuid():N}".ToUpperInvariant(),
                SupplierLot = request.SupplierLot?.Trim(),
                Unit = request.Unit,
                MaterialName = request.MaterialName,
                SupplierName = request.SupplierName,
                CurrentQuantity = request.Quantity,
                PhysicalState = request.PhysicalState,
                ExpiryDate = request.ExpiryDate,
                IsQCApproved = false // لازم تتفحص الأول من المعمل
            };

            await _unitOfWork.RawMaterials.AddAsync(material);

            // Finding #4: كان فيه SaveChangesAsync هنا قبل كده عشان "نجيب الـ Id الأول عشان
            // الـ Audit" — لكن الـ Audit بيستخدم MaterialName (نص) مش Id أصلاً، فالحفظ
            // المزدوج كان غير ضروري فعليًا وبيكسر مبدأ "العملية الذرية الواحدة".
            // دلوقتي: تسجيل الخامة + الـ Audit بيتحفظوا سوا في نفس الـ SaveChangesAsync.
            await _auditService.LogAsync(userId, "Received", "RawMaterial", material.LotCode,
                $"كمية: {material.CurrentQuantity}, حالة: {material.PhysicalState}");

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("تم استلام خامة جديدة {MaterialName} بكمية {Quantity}", material.MaterialName, material.CurrentQuantity);

            return MapToDto(material);
        }

        public async Task<PagedResult<RawMaterialResponseDto>> GetAllMaterialsAsync(PaginationParams pagination)
        {
            // القسم 4.9: التجميع والترقيم بيحصلوا على مستوى قاعدة البيانات (COUNT + Skip/Take)
            // بدل تحميل كل الصفوف للذاكرة الأول.
            var query = _unitOfWork.RawMaterials.Query();

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderByDescending(m => m.Id)
                .Skip(pagination.Skip)
                .Take(pagination.PageSize)
                .ToListAsync();

            return new PagedResult<RawMaterialResponseDto>
            {
                Items = items.Select(material => MapToDto(material)),
                Page = pagination.Page,
                PageSize = pagination.PageSize,
                TotalCount = totalCount
            };
        }

        public async Task<IEnumerable<RawMaterialResponseDto>> GetApprovedMaterialsAsync()
        {
            var materials = await _unitOfWork.RawMaterials.FindAsync(m => m.IsQCApproved && m.CurrentQuantity > 0 && m.ExpiryDate.Date >= DateTime.UtcNow.Date);
            return materials.Select(material => MapToDto(material));
        }

        public async Task<RawMaterialResponseDto> IssueMaterialAsync(IssueMaterialDto request, int userId)
        {
            var material = await _unitOfWork.RawMaterials.GetByIdAsync(request.RawMaterialId);

            if (material == null)
                throw NotFoundException.For("الخامة", "MATERIAL_NOT_FOUND");

            if (request.RequestId is Guid requestId)
            {
                var previous = await _db.MaterialIssues.SingleOrDefaultAsync(issue => issue.RequestId == requestId);
                if (previous is not null)
                {
                    if (previous.RawMaterialId != material.Id || previous.IssuedBy != userId ||
                        previous.Quantity != request.QuantityToIssue)
                        throw new ConflictException("معرّف الطلب مرتبط بعملية صرف مختلفة", "REQUEST_ID_CONFLICT");
                    return MapToDto(material, previous.Id);
                }
            }

            if (!material.IsQCApproved || material.ExpiryDate.Date < DateTime.UtcNow.Date)
                throw new BusinessRuleException("MATERIAL_NOT_QC_APPROVED", "الخامة غير معتمدة أو انتهت صلاحيتها");

            if (material.CurrentQuantity < request.QuantityToIssue)
                throw new BusinessRuleException("INSUFFICIENT_QUANTITY", "الكمية المطلوبة أكبر من المتاح في المخزن");

            var issue = new MaterialIssue
            {
                RawMaterialId = material.Id,
                RequestId = request.RequestId,
                Quantity = request.QuantityToIssue,
                IssuedBy = userId,
                IssuedAt = DateTime.UtcNow
            };
            _db.MaterialIssues.Add(issue);
            material.CurrentQuantity -= request.QuantityToIssue;
            _unitOfWork.RawMaterials.Update(material);

            await _auditService.LogAsync(userId, "Issued_To_Production", "RawMaterial", material.LotCode ?? material.MaterialName,
                $"كمية مصروفة: {request.QuantityToIssue}");

            await SaveWithConcurrencyCheckAsync(material.MaterialName);

            _logger.LogInformation("تم صرف {Qty} من الخامة {MaterialName} لخط الإنتاج", request.QuantityToIssue, material.MaterialName);

            return MapToDto(material, issue.Id);
        }

        public async Task<RawMaterialResponseDto> RecordMaterialWasteAsync(RecordMaterialWasteDto request, int userId)
        {
            var material = await _unitOfWork.RawMaterials.GetByIdAsync(request.RawMaterialId);

            if (material == null)
                throw NotFoundException.For("الخامة", "MATERIAL_NOT_FOUND");

            if (material.CurrentQuantity < request.WastedQuantity)
                throw new BusinessRuleException("INSUFFICIENT_QUANTITY", "الكمية المطلوب تسجيلها كهالك أكبر من المتاح فعليًا");

            material.CurrentQuantity -= request.WastedQuantity;
            _unitOfWork.RawMaterials.Update(material);

            var scrapEntry = new ScrapAndQuarantine
            {
                SourceType = "RawMaterial",
                ReferenceId = material.Id,
                Quantity = request.WastedQuantity,
                ScrapReason = request.Reason,
                FinalDecision = "Destroyed" // هالك مخزني نهائي، مفيش قرار تدوير أو عزل عليه
            };

            await _unitOfWork.ScrapAndQuarantines.AddAsync(scrapEntry);

            await _auditService.LogAsync(userId, "Material_Wasted", "RawMaterial", material.MaterialName,
                $"كمية هالك: {request.WastedQuantity}, السبب: {request.Reason}");

            await SaveWithConcurrencyCheckAsync(material.MaterialName); // خصم الرصيد + تسجيل الهالك + الـ Audit سوا

            _logger.LogInformation("تم تسجيل هالك مخزني {Qty} من {MaterialName}", request.WastedQuantity, material.MaterialName);

            return MapToDto(material);
        }

        /// <summary>
        /// Finding #5: بعد إضافة RowVersion على RawMaterial، أي تعديل على الكمية ممكن
        /// يصطدم بتعارض تزامن لو طلب تاني عدّل نفس الصف في نفس اللحظة. بنحوّل
        /// DbUpdateConcurrencyException لرسالة عربية واضحة بدل ما توصل كاستثناء تقني خام.
        /// </summary>
        private async Task SaveWithConcurrencyCheckAsync(string materialName)
        {
            try
            {
                await _unitOfWork.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex, "تعارض تزامن أثناء تعديل كمية الخامة {MaterialName}", materialName);
                throw new ConflictException(
                    "الكمية اتغيّرت من عملية تانية في نفس اللحظة، من فضلك أعد المحاولة",
                    "CONCURRENCY_CONFLICT");
            }
        }

        private static RawMaterialResponseDto MapToDto(RawMaterial m, int? issueId = null)
        {
            return new RawMaterialResponseDto
            {
                Id = m.Id,
                LotCode = m.LotCode,
                SupplierLot = m.SupplierLot,
                Unit = m.Unit,
                IssueId = issueId,
                MaterialName = m.MaterialName,
                SupplierName = m.SupplierName,
                CurrentQuantity = m.CurrentQuantity,
                PhysicalState = m.PhysicalState,
                ExpiryDate = m.ExpiryDate,
                IsQCApproved = m.IsQCApproved
            };
        }
    }
}
