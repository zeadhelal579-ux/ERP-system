using ERP_system.Common;
using ERP_system.DTOs.Scrap;
using ERP_system.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP_system.Controllers
{
    // ==========================================================================
    // اكتشاف جديد أثناء المراجعة (مش من ضمن جدول 4.11 الأصلي): الكود القديم كان حاطط
    // [Authorize(Roles = "QualityManager,SuperAdmin")] على مستوى الكلاس بالكامل، يعني
    // بيتطبّق حرفيًا على approve-rework كمان! ده يخالف نص رؤية المشروع صراحة:
    // "طلب إعادة تدوير: يحتاج تصعيدًا وموافقة من المدير العام تحديدًا (وليس أي مدير آخر)"
    // — بمعنى إن مدير الجودة (اللي أصلاً هو من رفض التشغيلة) كان يقدر يوافق على تصعيده
    // هو نفسه لإعادة التدوير، وده يُفرغ مبدأ "التصعيد" من معناه تمامًا. دلوقتي كل
    // Endpoint له صلاحيته الدقيقة بشكل منفصل بدل تعميم واحد على كل الكلاس.
    // ==========================================================================
    [Route("api/v1/[controller]")]
    [Authorize]
    public class ScrapController : BaseApiController
    {
        private readonly IScrapService _scrapService;

        public ScrapController(IScrapService scrapService)
        {
            _scrapService = scrapService;
        }

        [Authorize(Roles = "QualityManager,SuperAdmin")]
        [HttpPost("quality-decision")]
        public async Task<IActionResult> QualityDecision([FromBody] ScrapDecisionDto request)
        {
            var result = await _scrapService.QualityDecisionAsync(request, CurrentUserId);
            return Success(result, "تم تسجيل قرار مدير الجودة بنجاح");
        }

        // الإصلاح: دلوقتي مقصورة فعليًا على GeneralManager (+ SuperAdmin للإدارة التقنية)
        // بس، مطابقة تمامًا لمبدأ "التصعيد" الموثّق في رؤية المشروع.
        [Authorize(Roles = "GeneralManager,SuperAdmin")]
        [HttpPost("approve-rework/{scrapEntryId}")]
        public async Task<IActionResult> ApproveRework(int scrapEntryId)
        {
            var result = await _scrapService.ApproveReworkAsync(scrapEntryId, CurrentUserId);
            return Success(result, "تمت الموافقة على إعادة التدوير، وعادت التشغيلة لخط الإنتاج");
        }

        [Authorize(Roles = "QualityManager,GeneralManager,SuperAdmin")]
        [HttpGet("pending")]
        public async Task<IActionResult> GetPendingEntries()
        {
            var result = await _scrapService.GetPendingEntriesAsync();
            return Success(result);
        }

        [Authorize(Roles = "QualityManager,GeneralManager,SuperAdmin")]
        [HttpGet("all")]
        public async Task<IActionResult> GetAllEntries([FromQuery] PaginationParams pagination)
        {
            var result = await _scrapService.GetAllEntriesAsync(pagination);
            return Success(result);
        }
    }
}
