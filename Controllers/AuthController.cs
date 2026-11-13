using ERP_system.Common;
using ERP_system.DTOs.Auth;
using ERP_system.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ERP_system.Controllers
{
    [Route("api/v1/[controller]")] // القسم 4.10: API Versioning
    public class AuthController : BaseApiController
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        // القسم 6 (End-to-End Security Checklist): Rate Limiting على تسجيل الدخول كان
        // مفقود تمامًا - دلوقتي مضاف عبر Middleware .NET المدمج (بدون أي حزمة خارجية)
        // لمنع محاولات تخمين كلمة المرور المتكررة. راجع تعريف السياسة "login" في Program.cs.
        [EnableRateLimiting("login")]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            var result = await _authService.LoginAsync(request);
            return Success(result);
        }

        [Authorize(Roles = "SuperAdmin")]
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
        {
            await _authService.RegisterAsync(request);
            return SuccessMessage("تم إنشاء المستخدم بنجاح");
        }
    }
}
