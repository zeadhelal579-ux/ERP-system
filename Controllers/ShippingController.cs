using ERP_system.Common;
using ERP_system.DTOs.Shipping;
using ERP_system.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP_system.Controllers
{
    [Route("api/v1/[controller]")]
    [Authorize]
    public class ShippingController : BaseApiController
    {
        private readonly IShippingService _shippingService;

        public ShippingController(IShippingService shippingService)
        {
            _shippingService = shippingService;
        }

        [Authorize(Roles = "Storekeeper,SuperAdmin")]
        [HttpPost("create")]
        public async Task<IActionResult> CreateShipment([FromBody] CreateShipmentDto request)
        {
            var result = await _shippingService.CreateShipmentAsync(request, CurrentUserId);
            return Success(result);
        }

        [Authorize(Roles = "Storekeeper,SuperAdmin,GeneralManager")]
        [HttpGet("ready")]
        public async Task<IActionResult> GetReadyForShipping()
        {
            var result = await _shippingService.GetReadyForShippingAsync();
            return Success(result);
        }

        [Authorize(Roles = "Storekeeper,SuperAdmin,GeneralManager")]
        [HttpGet("all")]
        public async Task<IActionResult> GetAllShipments([FromQuery] PaginationParams pagination)
        {
            var result = await _shippingService.GetAllShipmentsAsync(pagination);
            return Success(result);
        }
    }
}
