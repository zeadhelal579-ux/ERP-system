namespace ERP_system.DTOs.Scrap
{
    public class ScrapResponseDto
    {
        public int Id { get; set; }
        public string SourceType { get; set; } = string.Empty;
        public int ReferenceId { get; set; }
        public decimal Quantity { get; set; }
        public string ScrapReason { get; set; } = string.Empty;
        public string FinalDecision { get; set; } = string.Empty;

        // إضافة متعمدة (مش موجودة في الأصل): مفيدة لأي شاشة عزل/هالك في الفرونت
        // عشان ترتب أو تعرض تاريخ إنشاء السطر - إضافة خالصة (Additive) ومفيهاش أي كسر
        // للتوافق مع أي استخدام قائم.
        public DateTime CreatedAt { get; set; }
    }
}
