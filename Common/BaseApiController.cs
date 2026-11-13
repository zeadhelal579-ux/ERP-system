using Microsoft.AspNetCore.Mvc;

namespace ERP_system.Common
{
    /// <summary>
    /// القسم 4.2 (Common/BaseApiController.cs) — كل الـ Controllers ترث من هنا بدل ControllerBase
    /// مباشرة. بتحل مشكلتين موثقتين في القسم 4.11:
    ///   - Finding #11: تكرار GetCurrentUserId() في كل Controller.
    ///   - Finding #4.3: كل Controller كان بيرجّع Shape مختلف؛ دلوقتي في هيلبرز جاهزة
    ///     بترجّع ApiResponse&lt;T&gt; موحّد دايمًا.
    /// ملاحظة مهمة: دي مجرد راحة في الكتابة (DX) — الاستثناءات المخصصة (Exceptions/) هي
    /// اللي بتتحكم فعليًا في حالات الفشل عبر الـ Middleware، مش الهيلبرز دي.
    /// </summary>
    [ApiController]
    public abstract class BaseApiController : ControllerBase
    {
        /// <summary>الـ Id بتاع المستخدم صاحب التوكن الحالي</summary>
        protected int CurrentUserId => User.GetUserId();

        /// <summary>يبني استجابة نجاح 200 بغلاف ApiResponse&lt;T&gt; موحّد</summary>
        protected IActionResult Success<T>(T data, string? message = null) =>
            Ok(ApiResponse<T>.Ok(data, message));

        /// <summary>يبني استجابة نجاح 200 من غير جسم بيانات (لعمليات زي "تم الصرف بنجاح")</summary>
        protected IActionResult SuccessMessage(string message) =>
            Ok(ApiResponse.Ok(message));

        /// <summary>يبني استجابة 201 Created بغلاف ApiResponse&lt;T&gt; موحّد</summary>
        protected IActionResult SuccessCreated<T>(string actionName, object routeValues, T data, string? message = null) =>
            CreatedAtAction(actionName, routeValues, ApiResponse<T>.Ok(data, message));
    }
}
