using System.Net;

namespace ERP_system.Exceptions
{
    /// <summary>
    /// يُطلق لما فيه محاولة نقل كيان (تشغيلة غالبًا) من حالة لحالة تانية بترتيب غير مسموح
    /// بآلة الحالات الموثقة في القسم 3.4 من التصميم المعماري.
    /// الـ Middleware بيحوّله لـ HTTP 409 Conflict (مش 400، لأن الطلب نفسه سليم شكليًا،
    /// لكن حالة الكيان الحالية بترفضه).
    /// </summary>
    public class InvalidStateTransitionException : AppException
    {
        public InvalidStateTransitionException(string message, string errorCode = "INVALID_STATE_TRANSITION")
            : base(HttpStatusCode.Conflict, errorCode, message)
        {
        }
    }
}
