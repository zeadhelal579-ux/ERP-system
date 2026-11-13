using System.Net;

namespace ERP_system.Exceptions
{
    /// <summary>
    /// يُطلق عند تعارض تزامن (Optimistic Concurrency عبر RowVersion) أو تعارض قيد فريد
    /// (زي اسم مستخدم أو رقم تشغيلة اتسجل في نفس اللحظة من طلب تاني).
    /// الـ Middleware بيحوّله لـ HTTP 409 Conflict.
    /// </summary>
    public class ConflictException : AppException
    {
        public ConflictException(string message, string errorCode = "CONFLICT")
            : base(HttpStatusCode.Conflict, errorCode, message)
        {
        }
    }
}
