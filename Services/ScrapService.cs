using ERP_system.Common;
using ERP_system.DTOs.Production;
using ERP_system.DTOs.Scrap;
using ERP_system.Exceptions;
using ERP_system.Models;
using ERP_system.Repositories;
using ERP_system.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ERP_system.Services
{
    public class ScrapService : IScrapService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;
        private readonly ILogger<ScrapService> _logger;

        public ScrapService(IUnitOfWork unitOfWork, IAuditService auditService, ILogger<ScrapService> logger)
        {
            _unitOfWork = unitOfWork;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task<ScrapResponseDto> QualityDecisionAsync(ScrapDecisionDto request, int userId)
        {
            var entry = await _unitOfWork.ScrapAndQuarantines.GetByIdAsync(request.ScrapEntryId);

            if (entry == null)
                throw NotFoundException.For("سطر العزل/الهالك", "SCRAP_ENTRY_NOT_FOUND");

            if (entry.FinalDecision != "Pending")
                throw new InvalidStateTransitionException(
                    $"تم اتخاذ قرار على هذا السطر بالفعل ('{entry.FinalDecision}')");

            if (request.Decision != "Destroy" && request.Decision != "RequestRework")
                throw new BusinessRuleException("INVALID_DECISION_VALUE",
                    "القرار لازم يكون 'Destroy' أو 'RequestRework' بس");

            entry.DecisionBy = userId;

            if (request.Decision == "Destroy")
            {
                entry.FinalDecision = "Destroyed";

                // ==========================================================================
                // Finding #6 (🟡 القسم 4.11) — الإصلاح الرئيسي: قبل كده، قرار "إعدام" كان
                // بيحدّث FinalDecision بس على سطر العزل، ويسيب ProductionBatch.CurrentState
                // عالقة على "Quarantine" للأبد — وده كان بيضخّم عدّاد QuarantineCount في لوحة
                // المدير العام لأنه بيحسب تشغيلات اتعدمت فعليًا كإنها لسه معزولة.
                // دلوقتي: أي قرار إعدام على سطر مصدره "ManagerRejection" (يعني ReferenceId
                // بتاعه فعلاً Id تشغيلة) بيحدّث حالة التشغيلة نفسها لـ "Destroyed" كمان،
                // في نفس الـ Transaction/SaveChangesAsync الواحد.
                // ملاحظة: سطور SourceType == "RawMaterial" أو "ProductionLine" بتتسجّل
                // "Destroyed" مباشرة من مصدرها (StoreService/ProductionService) ومبتوصلش
                // لهنا أصلاً لأنها مش "Pending" من الأول، فمفيش لبس في تفسير ReferenceId.
                // ==========================================================================
                if (entry.SourceType == "ManagerRejection")
                {
                    var batch = await _unitOfWork.ProductionBatches.GetByIdAsync(entry.ReferenceId);
                    if (batch != null)
                    {
                        batch.CurrentState = "Destroyed";
                        _unitOfWork.ProductionBatches.Update(batch);
                    }
                }

                await _auditService.LogAsync(userId, "Scrap_Destroyed", entry.SourceType, entry.ReferenceId.ToString(), entry.ScrapReason);
            }
            else // RequestRework
            {
                entry.FinalDecision = "Sent_To_Rework"; // لسه محتاج خطوة الموافقة النهائية من المدير العام
                await _auditService.LogAsync(userId, "Rework_Requested", entry.SourceType, entry.ReferenceId.ToString(), entry.ScrapReason);
            }

            _unitOfWork.ScrapAndQuarantines.Update(entry);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("قرار جودة على سطر العزل #{Id}: {Decision}", entry.Id, entry.FinalDecision);

            return MapToDto(entry);
        }

        public async Task<BatchResponseDto> ApproveReworkAsync(int scrapEntryId, int userId)
        {
            var entry = await _unitOfWork.ScrapAndQuarantines.GetByIdAsync(scrapEntryId);

            if (entry == null)
                throw NotFoundException.For("سطر العزل/الهالك", "SCRAP_ENTRY_NOT_FOUND");

            if (entry.FinalDecision != "Sent_To_Rework")
                throw new InvalidStateTransitionException(
                    $"لا يمكن الموافقة على إعادة التدوير وحالة الطلب '{entry.FinalDecision}'");

            if (entry.SourceType != "ManagerRejection")
                throw new BusinessRuleException("INVALID_REWORK_SOURCE",
                    "إعادة التدوير مسموحة بس للتشغيلات المرفوضة من مدير الجودة");

            var batch = await _unitOfWork.ProductionBatches.GetByIdAsync(entry.ReferenceId);
            if (batch == null)
                throw NotFoundException.For("التشغيلة المرتبطة بطلب إعادة التدوير", "BATCH_NOT_FOUND");

            entry.FinalDecision = "Reworked";
            _unitOfWork.ScrapAndQuarantines.Update(entry);

            batch.CurrentState = "Rework_In_Progress";
            batch.IsReworked = true;
            _unitOfWork.ProductionBatches.Update(batch);

            await _auditService.LogAsync(userId, "Rework_Approved", "Batch", batch.BatchNumber, null);

            try
            {
                await _unitOfWork.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex, "تعارض تزامن أثناء الموافقة على إعادة تدوير التشغيلة {BatchNumber}", batch.BatchNumber);
                throw new ConflictException(
                    "التشغيلة اتعدّلت من عملية تانية في نفس اللحظة، من فضلك أعد المحاولة",
                    "CONCURRENCY_CONFLICT");
            }

            _logger.LogInformation("تمت الموافقة على إعادة تدوير التشغيلة {BatchNumber}", batch.BatchNumber);

            return BatchMapper.ToDto(batch);
        }

        public async Task<IEnumerable<ScrapResponseDto>> GetPendingEntriesAsync()
        {
            // بيشمل "Pending" (محتاج قرار مدير الجودة) و"Sent_To_Rework" (محتاج موافقة
            // المدير العام النهائية) — الاتنين لسه "معلّقين" ومحتاجين حد ياخد إجراء.
            var entries = await _unitOfWork.ScrapAndQuarantines.FindAsync(
                e => e.FinalDecision == "Pending" || e.FinalDecision == "Sent_To_Rework");
            return entries.Select(MapToDto);
        }

        public async Task<PagedResult<ScrapResponseDto>> GetAllEntriesAsync(PaginationParams pagination)
        {
            var query = _unitOfWork.ScrapAndQuarantines.Query();

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderByDescending(e => e.Id)
                .Skip(pagination.Skip)
                .Take(pagination.PageSize)
                .ToListAsync();

            return new PagedResult<ScrapResponseDto>
            {
                Items = items.Select(MapToDto),
                Page = pagination.Page,
                PageSize = pagination.PageSize,
                TotalCount = totalCount
            };
        }

        private static ScrapResponseDto MapToDto(ScrapAndQuarantine e)
        {
            return new ScrapResponseDto
            {
                Id = e.Id,
                SourceType = e.SourceType,
                ReferenceId = e.ReferenceId,
                Quantity = e.Quantity,
                ScrapReason = e.ScrapReason,
                FinalDecision = e.FinalDecision,
                CreatedAt = e.CreatedAt
            };
        }
    }
}
