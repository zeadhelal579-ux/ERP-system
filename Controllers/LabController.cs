using ERP_system.Common;
using ERP_system.DTOs.Lab;
using ERP_system.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP_system.Controllers
{
    [Route("api/v1/[controller]")]
    [Authorize]
    public class LabController : BaseApiController
    {
        private readonly ILabService _labService;

        public LabController(ILabService labService)
        {
            _labService = labService;
        }

        [Authorize(Roles = "LabTech,SuperAdmin")]
        [HttpPost("submit-result")]
        public async Task<IActionResult> SubmitResult([FromBody] SubmitLabResultDto request)
        {
            var result = await _labService.SubmitResultAsync(request, CurrentUserId);
            return Success(result);
        }

        [Authorize(Roles = "LabTech,SuperAdmin,GeneralManager")]
        [HttpGet("results")]
        public async Task<IActionResult> GetAllResults([FromQuery] PaginationParams pagination)
        {
            var result = await _labService.GetAllResultsAsync(pagination);
            return Success(result);
        }

        [Authorize(Roles = "LabTech,SuperAdmin,GeneralManager")]
        [HttpGet("pending-materials")]
        public async Task<IActionResult> GetPendingMaterials()
        {
            var result = await _labService.GetPendingIncomingMaterialsAsync();
            return Success(result);
        }

        [Authorize(Roles = "LabTech,SuperAdmin,GeneralManager")]
        [HttpGet("pending-batches")]
        public async Task<IActionResult> GetPendingBatches()
        {
            var result = await _labService.GetPendingBatchesAsync();
            return Success(result);
        }
    }
}
