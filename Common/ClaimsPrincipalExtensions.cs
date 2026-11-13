using System.Security.Claims;

namespace ERP_system.Common
{
    /// <summary>
    /// القسم 4.2 + Finding #11 في التصميم المعماري: كل الـ Controllers (Lab, Manager, Production,
    /// Scrap, Shipping, Store) كانت بتكرر نفس دالة GetCurrentUserId() حرفيًا في كل ملف.
    /// دلوقتي المنطق موجود مرة واحدة بس هنا، ومستخدَم عبر BaseApiController.
    /// </summary>
    public static class ClaimsPrincipalExtensions
    {
        /// <summary>
        /// بيرجع الـ Id بتاع المستخدم الحالي من الـ JWT Claims.
        /// لو الـ Claim مش موجود أو مش رقم صحيح، يرجّع 0 (نفس السلوك القديم بالظبط
        /// عشان مانكسرش أي منطق قائم يعتمد عليه، لكن دلوقتي في مكان واحد بس قابل للتعديل).
        /// </summary>
        public static int GetUserId(this ClaimsPrincipal user)
        {
            var claim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(claim, out var id) ? id : 0;
        }

        /// <summary>بيرجع اسم اليوزر (Username) من الـ Claims، أو null لو مش موجود</summary>
        public static string? GetUsername(this ClaimsPrincipal user) =>
            user.FindFirst(ClaimTypes.Name)?.Value;

        /// <summary>بيرجع الدور (Role) من الـ Claims، أو null لو مش موجود</summary>
        public static string? GetRole(this ClaimsPrincipal user) =>
            user.FindFirst(ClaimTypes.Role)?.Value;
    }
}
