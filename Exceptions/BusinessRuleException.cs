using System.Net;

namespace ERP_system.Exceptions
{
    /// <summary>
    /// يُطلق لأي مخالفة لقاعدة عمل حقيقية (Domain Rule) مش مجرد تحقق شكلي من المدخلات:
    /// كمية أكبر من المتاح، خامة لسه مش معتمدة من المعمل، مرجع غير منطقي...إلخ.
    /// الـ Middleware بيحوّله لـ HTTP 400 Bad Request مع ErrorCode واضح للفرونت.
    /// </summary>
    public class BusinessRuleException : AppException
    {
        public BusinessRuleException(string errorCode, string message)
            : base(HttpStatusCode.BadRequest, errorCode, message)
        {
        }
    }
}
