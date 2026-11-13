using ERP_system.Common;
using ERP_system.DTOs.Production;
using ERP_system.DTOs.Shipping;

namespace ERP_system.Services.Interfaces
{
    public interface IShippingService
    {
        /// <summary>يطلق NotFoundException أو InvalidStateTransitionException حسب سبب الفشل</summary>
        Task<ShipmentResponseDto> CreateShipmentAsync(CreateShipmentDto request, int userId);

        Task<IEnumerable<BatchSummaryDto>> GetReadyForShippingAsync();
        Task<PagedResult<ShipmentResponseDto>> GetAllShipmentsAsync(PaginationParams pagination);
    }
}
