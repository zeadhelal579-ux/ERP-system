using ERP_system.DTOs.Manager;
using ERP_system.DTOs.Production;

namespace ERP_system.Services.Interfaces
{
    public interface IManagerService
    {
        /// <summary>يطلق NotFoundException أو InvalidStateTransitionException حسب سبب الفشل. بيرجّع التشغيلة بعد التحديث.</summary>
        Task<BatchResponseDto> MakeDecisionAsync(ManagerDecisionDto request, int userId);

        Task<IEnumerable<BatchSummaryDto>> GetPendingApprovalsAsync();
    }
}
