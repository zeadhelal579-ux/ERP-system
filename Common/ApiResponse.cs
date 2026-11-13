namespace ERP_system.Common
{
    /// <summary>
    /// غلاف استجابة موحّد لكل الـ API — القسم 4.3 من التصميم المعماري.
    /// كل Endpoint يرجّع نفس الـ Shape بالظبط، فالفرونت في React يقدر يعرّف نوع TypeScript
    /// عام ApiResponse&lt;T&gt; واحد ويبني عليه كل الـ hooks، بدل التعامل مع Shape مختلف لكل Endpoint.
    /// </summary>
    /// <typeparam name="T">نوع البيانات الفعلية اللي بترجع عند النجاح</typeparam>
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }

        /// <summary>
        /// كود ثابت يقدر الفرونت ياخد قرار برمجي بناءً عليه (مثلاً: إخفاء زر معيّن)
        /// بدل تحليل نص الرسالة العربي. مثال: "BATCH_NOT_FOUND", "INVALID_STATE_TRANSITION".
        /// null في حالة النجاح.
        /// </summary>
        public string? ErrorCode { get; set; }

        /// <summary>نص عربي قابل للعرض مباشرة للمستخدم</summary>
        public string? Message { get; set; }

        public static ApiResponse<T> Ok(T data, string? message = null) => new()
        {
            Success = true,
            Data = data,
            Message = message
        };

        public static ApiResponse<T> Fail(string errorCode, string message) => new()
        {
            Success = false,
            ErrorCode = errorCode,
            Message = message
        };
    }

    /// <summary>
    /// نسخة بدون بيانات (Data) للـ Endpoints اللي بترجع نجاح/فشل بس من غير جسم بيانات
    /// (زي "تم صرف الخامة بنجاح")، عشان نتجنب استخدام ApiResponse&lt;object?&gt; في كل مكان.
    /// </summary>
    public class ApiResponse : ApiResponse<object?>
    {
        public static ApiResponse<object?> Ok(string? message = null) => new()
        {
            Success = true,
            Message = message
        };
    }
}
