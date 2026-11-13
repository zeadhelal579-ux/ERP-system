using ERP_system.DTOs.Manager;
using ERP_system.Common;
using ERP_system.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP_system.Controllers
{
    [Route("api/v1/[controller]")]
    [Authorize(Roles = "QualityManager,SuperAdmin")]
    public class ManagerController : BaseApiController
    {
        private readonly IManagerService _managerService;

        public ManagerController(IManagerService managerService)
        {
            _managerService = managerService;
        }

        [HttpPost("decision")]
        public async Task<IActionResult> MakeDecision([FromBody] ManagerDecisionDto request)
        {
            var result = await _managerService.MakeDecisionAsync(request, CurrentUserId);
            return Success(result, request.Approve ? "تمت الموافقة على التشغيلة" : "تم رفض التشغيلة وتحويلها للعزل");
        }

        [HttpGet("pending-approvals")]
        public async Task<IActionResult> GetPendingApprovals()
        {
            var result = await _managerService.GetPendingApprovalsAsync();
            return Success(result);
        }
    }
}
