using System.Net;

namespace ERP_system.Exceptions
{
    /// <summary>
    /// الأساس المشترك لكل الاستثناءات المخصصة في النظام.
    /// كل استثناء بيحمل: كود HTTP المناسب، ErrorCode ثابت يقدر الفرونت ياخد قرار بناءً عليه،
    /// ورسالة عربية جاهزة للعرض المباشر للمستخدم.
    /// (يطبّق توصية القسم 4.4 في التصميم المعماري: استثناءات مخصصة بدل إرجاع null/false من الـ Service)
    /// </summary>
    public abstract class AppException : Exception
    {
        public HttpStatusCode StatusCode { get; }
        public string ErrorCode { get; }

        protected AppException(HttpStatusCode statusCode, string errorCode, string message)
            : base(message)
        {
            StatusCode = statusCode;
            ErrorCode = errorCode;
        }
    }
}
