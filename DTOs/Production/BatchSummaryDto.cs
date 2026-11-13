namespace ERP_system.DTOs.Production
{
    /// <summary>
    /// شكل مختصر للتشغيلة، مُستخدَم في قوائم "قيد الانتظار" عبر أكتر من قسم
    /// (فحص المعمل، موافقة مدير الجودة، جاهز للشحن). كان في الكود الأصلي بيترجع
    /// كـ "IEnumerable&lt;object&gt;" بأشكال Anonymous Types مختلفة شوية من قسم لتاني —
    /// وده كان بيمنع Swagger/OpenAPI من توليد نوع TypeScript صحيح ليه (القسم 5.3)،
    /// لأن الـ Anonymous Types مبتظهرش في مخطط OpenAPI بشكل مفيد. دلوقتي شكل واحد
    /// موحّد وواضح قابل للتوليد التلقائي في الفرونت.
    /// </summary>
    public class BatchSummaryDto
    {
        public string? CurrentState { get; set; }
        public bool IsHeld { get; set; }
        public int Id { get; set; }
        public string BatchNumber { get; set; } = string.Empty;
        public int ProductId { get; set; }
        public decimal ProducedQty { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
