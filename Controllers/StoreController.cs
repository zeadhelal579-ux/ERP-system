using ERP_system.Common;
using ERP_system.DTOs.Store;
using ERP_system.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP_system.Controllers
{
    [Route("api/v1/[controller]")]
    [Authorize] // أي حد عنده توكن صالح يقدر يوصل للكلاس، والتحديد الدقيق على كل Endpoint
    public class StoreController : BaseApiController
    {
        private readonly IStoreService _storeService;

        public StoreController(IStoreService storeService)
        {
            _storeService = storeService;
        }

        [Authorize(Roles = "Storekeeper,SuperAdmin")]
        [HttpPost("add-material")]
        public async Task<IActionResult> AddMaterial([FromBody] AddRawMaterialDto request)
        {
            var result = await _storeService.AddRawMaterialAsync(request, CurrentUserId);
            return Success(result);
        }

        [Authorize(Roles = "Storekeeper,SuperAdmin,GeneralManager")]
        [HttpGet("materials")]
        public async Task<IActionResult> GetAllMaterials([FromQuery] PaginationParams pagination)
        {
            var result = await _storeService.GetAllMaterialsAsync(pagination);
            return Success(result);
        }

        [Authorize(Roles = "Storekeeper,SuperAdmin,GeneralManager")]
        [HttpGet("materials/approved")]
        public async Task<IActionResult> GetApprovedMaterials()
        {
            var result = await _storeService.GetApprovedMaterialsAsync();
            return Success(result);
        }

        [Authorize(Roles = "Storekeeper,SuperAdmin")]
        [HttpPost("issue-material")]
        public async Task<IActionResult> IssueMaterial([FromBody] IssueMaterialDto request)
        {
            var result = await _storeService.IssueMaterialAsync(request, CurrentUserId);
            return Success(result, "تم صرف الخامة لخط الإنتاج بنجاح");
        }

        [Authorize(Roles = "Storekeeper,SuperAdmin")]
        [HttpPost("record-waste")]
        public async Task<IActionResult> RecordWaste([FromBody] RecordMaterialWasteDto request)
        {
            var result = await _storeService.RecordMaterialWasteAsync(request, CurrentUserId);
            return Success(result, "تم تسجيل هالك المخزن بنجاح");
        }
    }
}
