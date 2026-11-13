using ERP_system.Common;
using ERP_system.Exceptions;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Text.Json;

namespace ERP_system.Middlewares
{
    /// <summary>
    /// القسم 4.4 من التصميم المعماري: بدل ما كل Service يرجّع null/false عند الفشل ويضطر كل
    /// Controller يحوّلها يدويًا لرسالة عامة (وده بيفقد "السبب الحقيقي" للفشل)، دلوقتي
    /// الـ Services بترمي استثناءات مخصصة (Exceptions/*.cs)، والـ Middleware ده بيمسكها
    /// كلها في مكان واحد ويحوّلها تلقائيًا لكود HTTP الصحيح (404/409/400) + ApiResponse&lt;T&gt;
    /// موحّد بـ ErrorCode واضح يقدر الفرونت ياخد قرار بناءً عليه.
    ///
    /// كمان بيمسك استثناءات EF Core التقنية (DbUpdateConcurrencyException, DbUpdateException)
    /// كخط دفاع أخير للـ Concurrency (Finding #5) والفهارس الفريدة (Finding #7)، حتى لو
    /// Service معيّن نسي يحوّطها بمحاولة try/catch محلية.
    /// </summary>
    public class ExceptionHandlerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlerMiddleware> _logger;
        private readonly IHostEnvironment _environment;

        public ExceptionHandlerMiddleware(RequestDelegate next, ILogger<ExceptionHandlerMiddleware> logger, IHostEnvironment environment)
        {
            _next = next;
            _logger = logger;
            _environment = environment;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (AppException appEx)
            {
                _logger.LogWarning(appEx, "استثناء أعمال متوقع: {ErrorCode}", appEx.ErrorCode);
                await WriteResponseAsync(context, (int)appEx.StatusCode, appEx.ErrorCode, appEx.Message);
            }
            catch (DbUpdateConcurrencyException concurrencyEx)
            {
                _logger.LogWarning(concurrencyEx, "تعارض تزامن (Concurrency) لم يُمسك على مستوى الـ Service");
                await WriteResponseAsync(context, (int)HttpStatusCode.Conflict, "CONCURRENCY_CONFLICT",
                    "البيانات اتغيّرت من عملية تانية في نفس الوقت، من فضلك أعد المحاولة");
            }
            catch (DbUpdateException dbEx)
            {
                // الأغلب: مخالفة فهرس Unique (Finding #7) لم تُمسك مسبقًا على مستوى الـ Service
                _logger.LogWarning(dbEx, "خطأ حفظ في قاعدة البيانات (على الأرجح تعارض قيد فريد)");
                await WriteResponseAsync(context, (int)HttpStatusCode.Conflict, "DATA_CONFLICT",
                    "تعارض في البيانات (مثال: قيمة مفروض تكون فريدة ومُستخدمة بالفعل)");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "استثناء غير متوقع");
                var detail = _environment.IsDevelopment() ? ex.Message : "حدث خطأ غير متوقع، من فضلك حاول لاحقًا";
                await WriteResponseAsync(context, (int)HttpStatusCode.InternalServerError, "INTERNAL_ERROR", detail);
            }
        }

        private static async Task WriteResponseAsync(HttpContext context, int statusCode, string errorCode, string message)
        {
            if (context.Response.HasStarted)
                return;

            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";

            var response = ApiResponse<object?>.Fail(errorCode, message);
            var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(json);
        }
    }

    public static class ExceptionHandlerMiddlewareExtensions
    {
        public static IApplicationBuilder UseAppExceptionHandler(this IApplicationBuilder app)
        {
            return app.UseMiddleware<ExceptionHandlerMiddleware>();
        }
    }
}
