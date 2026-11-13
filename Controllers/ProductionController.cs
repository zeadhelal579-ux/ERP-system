using ERP_system.Common;
using ERP_system.DTOs.Production;
using ERP_system.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP_system.Controllers
{
    [Route("api/v1/[controller]")]
    [Authorize]
    public class ProductionController : BaseApiController
    {
        private readonly IProductionService _productionService;

        public ProductionController(IProductionService productionService)
        {
            _productionService = productionService;
        }

        [Authorize(Roles = "ProductionManager,SuperAdmin")]
        [HttpPost("start-batch")]
        public async Task<IActionResult> StartBatch([FromBody] StartBatchDto request)
        {
            var result = await _productionService.StartBatchAsync(request, CurrentUserId);
            return Success(result);
        }

        [Authorize(Roles = "ProductionManager,SuperAdmin")]
        [HttpPost("finish-batch")]
        public async Task<IActionResult> FinishBatch([FromBody] FinishBatchDto request)
        {
            var result = await _productionService.FinishBatchAsync(request, CurrentUserId);
            return Success(result);
        }

        [Authorize(Roles = "ProductionManager,SuperAdmin,GeneralManager")]
        [HttpGet("batches")]
        public async Task<IActionResult> GetAllBatches([FromQuery] PaginationParams pagination)
        {
            var result = await _productionService.GetAllBatchesAsync(pagination);
            return Success(result);
        }

        [Authorize(Roles = "ProductionManager,SuperAdmin,GeneralManager")]
        [HttpGet("batches/{id}")]
        public async Task<IActionResult> GetBatchById(int id)
        {
            var result = await _productionService.GetBatchByIdAsync(id);
            return Success(result);
        }
    }
}
