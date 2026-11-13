using System.Net;

namespace ERP_system.Exceptions
{
    /// <summary>
    /// يُطلق لما الكيان المطلوب (خامة، تشغيلة، منتج، مستخدم...) مش موجود أصلاً.
    /// الـ Middleware بيحوّله تلقائيًا لـ HTTP 404.
    /// </summary>
    public class NotFoundException : AppException
    {
        public NotFoundException(string errorCode, string message)
            : base(HttpStatusCode.NotFound, errorCode, message)
        {
        }

        public static NotFoundException For(string entityNameArabic, string errorCode) =>
            new NotFoundException(errorCode, $"{entityNameArabic} غير موجود");
    }
}
