using ERP_system.Common;
using ERP_system.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP_system.Controllers
{
    [Route("api/v1/[controller]")]
    [Authorize(Roles = "GeneralManager,SuperAdmin")]
    public class ReportsController : BaseApiController
    {
        private readonly IReportsService _reportsService;

        public ReportsController(IReportsService reportsService)
        {
            _reportsService = reportsService;
        }

        [HttpGet("traceability/{batchId}")]
        public async Task<IActionResult> GetTraceabilityById(int batchId)
        {
            var result = await _reportsService.GetBatchTraceabilityAsync(batchId);
            return Success(result);
        }

        [HttpGet("traceability/by-number/{batchNumber}")]
        public async Task<IActionResult> GetTraceabilityByNumber(string batchNumber)
        {
            var result = await _reportsService.GetBatchTraceabilityByNumberAsync(batchNumber);
            return Success(result);
        }

        [HttpGet("dashboard-stats")]
        public async Task<IActionResult> GetDashboardStats()
        {
            var result = await _reportsService.GetDashboardStatsAsync();
            return Success(result);
        }
    }
}
