using ERP_system.Common;
using ERP_system.DTOs.Production;

namespace ERP_system.Services.Interfaces
{
    public interface IProductionService
    {
        /// <summary>يطلق NotFoundException أو BusinessRuleException حسب سبب الفشل</summary>
        Task<BatchResponseDto> StartBatchAsync(StartBatchDto request, int userId);

        /// <summary>يطلق NotFoundException أو InvalidStateTransitionException أو BusinessRuleException حسب سبب الفشل</summary>
        Task<BatchResponseDto> FinishBatchAsync(FinishBatchDto request, int userId);

        Task<PagedResult<BatchResponseDto>> GetAllBatchesAsync(PaginationParams pagination);

        /// <summary>يطلق NotFoundException لو التشغيلة مش موجودة</summary>
        Task<BatchResponseDto> GetBatchByIdAsync(int id);
    }
}
