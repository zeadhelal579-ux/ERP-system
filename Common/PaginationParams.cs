namespace ERP_system.Common
{
    /// <summary>
    /// القسم 4.9: "إضافة ?page=1&amp;pageSize=20 كمعيار قياسي على كل Endpoint من نوع GetAll".
    /// يُستخدم كـ [FromQuery] في الـ Controllers.
    /// </summary>
    public class PaginationParams
    {
        private const int MaxPageSize = 100;
        private int _pageSize = 20;

        public int Page { get; set; } = 1;

        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = value <= 0 ? 20 : Math.Min(value, MaxPageSize);
        }

        /// <summary>عدد العناصر اللي المفروض تتخطاها (Skip) بناءً على رقم الصفحة</summary>
        public int Skip => (Math.Max(Page, 1) - 1) * PageSize;
    }
}
