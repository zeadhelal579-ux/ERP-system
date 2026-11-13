namespace ERP_system.Common
{
    /// <summary>
    /// شكل موحّد لأي قائمة مُرجّعة بترقيم صفحات (Pagination) — القسم 4.9.
    /// بيتحط جوه Data بتاعة ApiResponse&lt;PagedResult&lt;T&gt;&gt;.
    /// </summary>
    public class PagedResult<T>
    {
        public IEnumerable<T> Items { get; set; } = Enumerable.Empty<T>();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    }
}
