using System.Linq.Expressions;

namespace ERP_system.Repositories
{
    public interface IGenericRepository<T> where T : class
    {
        Task<T?> GetByIdAsync(int id);

        // القسم 4.9: Overload بيقبل Include Expressions عشان نحل مشكلة N+1 — بدل ما
        // ReportsService.BuildTraceabilityDto يجيب كل كيان مرتبط بطلب منفصل، بيجيبهم
        // كلهم في استعلام واحد بـ JOIN حقيقي.
        Task<T?> GetByIdAsync(int id, params Expression<Func<T, object>>[] includes);

        Task<IEnumerable<T>> GetAllAsync();
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, params Expression<Func<T, object>>[] includes);

        /// <summary>
        /// بيرجّع IQueryable خام (AsNoTracking) للاستخدام في التجميعات الثقيلة
        /// (COUNT/SUM/GROUP BY) والترقيم (Pagination) — عشان التنفيذ يحصل على مستوى
        /// قاعدة البيانات نفسها بدل تحميل كل الصفوف للذاكرة أولاً (القسم 4.9).
        /// بيُستخدم للقراءة فقط؛ للتعديل استخدم باقي methods الـ Repository العادية.
        /// </summary>
        IQueryable<T> Query();

        Task AddAsync(T entity);
        void Update(T entity);
        void Remove(T entity);
    }
}
