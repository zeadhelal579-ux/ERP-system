using ERP_system.Common;
using ERP_system.DTOs.Production;
using ERP_system.DTOs.Scrap;

namespace ERP_system.Services.Interfaces
{
    public interface IScrapService
    {
        /// <summary>يطلق NotFoundException أو BusinessRuleException حسب سبب الفشل. بيرجّع سطر العزل بعد التحديث.</summary>
        Task<ScrapResponseDto> QualityDecisionAsync(ScrapDecisionDto request, int userId);

        /// <summary>يطلق NotFoundException أو BusinessRuleException حسب سبب الفشل. بيرجّع التشغيلة بعد إرجاعها لخط الإنتاج.</summary>
        Task<BatchResponseDto> ApproveReworkAsync(int scrapEntryId, int userId);

        Task<IEnumerable<ScrapResponseDto>> GetPendingEntriesAsync();
        Task<PagedResult<ScrapResponseDto>> GetAllEntriesAsync(PaginationParams pagination);
    }
}
