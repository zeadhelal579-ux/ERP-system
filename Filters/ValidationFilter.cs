using ERP_system.Common;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ERP_system.Filters
{
    /// <summary>
    /// القسم 4.6: FluentValidation لقواعد أدق على مستوى المدخلات (مش قواعد الأعمال
    /// الحرجة اللي فضلت في الـ Service Layer عمدًا). الفلتر ده بيدور تلقائيًا على أي
    /// IValidator&lt;T&gt; مسجَّل للـ DTO المُستقبَل في أي Action، ولو لقى مخالفة بيرجّع
    /// 400 بغلاف ApiResponse&lt;T&gt; الموحّد قبل ما يوصل للـ Controller أصلاً.
    ///
    /// ملاحظة تصميم: بنستخدم IValidator (غير Generic) + ValidationContext&lt;object&gt; بدل
    /// حزمة FluentValidation.AspNetCore (المتوقفة/deprecated في الإصدارات الحديثة من
    /// FluentValidation) — عشان نتجنب الاعتماد على حزمة إضافية ممكن تعارضها مع نسخة
    /// FluentValidation الأساسية.
    /// </summary>
    public class ValidationFilter : IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            foreach (var argument in context.ActionArguments.Values)
            {
                if (argument == null)
                    continue;

                var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());
                if (context.HttpContext.RequestServices.GetService(validatorType) is not IValidator validator)
                    continue; // مفيش Validator مسجَّل لهذا النوع - تجاهل وكمّل عادي

                var validationContext = new ValidationContext<object>(argument);
                var result = await validator.ValidateAsync(validationContext);

                if (!result.IsValid)
                {
                    var messages = result.Errors.Select(e => e.ErrorMessage).Distinct();
                    var response = ApiResponse<object?>.Fail("VALIDATION_ERROR", string.Join(" | ", messages));
                    context.Result = new BadRequestObjectResult(response);
                    return;
                }
            }

            await next();
        }
    }
}
