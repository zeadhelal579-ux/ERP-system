using ERP_system.Common;
using ERP_system.DTOs.Lab;
using ERP_system.DTOs.Production;

namespace ERP_system.Services.Interfaces
{
    public interface ILabService
    {
        /// <summary>يطلق NotFoundException أو BusinessRuleException حسب سبب الفشل</summary>
        Task<LabResultResponseDto> SubmitResultAsync(SubmitLabResultDto request, int userId);

        Task<PagedResult<LabResultResponseDto>> GetAllResultsAsync(PaginationParams pagination);
        Task<IEnumerable<PendingMaterialDto>> GetPendingIncomingMaterialsAsync();
        Task<IEnumerable<BatchSummaryDto>> GetPendingBatchesAsync();
    }
}
