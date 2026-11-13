namespace ERP_system.DTOs.Lab
{
    /// <summary>
    /// شكل مختصر لخامة بانتظار فحص المعمل. نفس ملاحظة BatchSummaryDto: بديل
    /// عن Anonymous Type كان بيمنع توليد نوع TypeScript صحيح (القسم 5.3).
    /// </summary>
    public class PendingMaterialDto
    {
        public int Id { get; set; }
        public string MaterialName { get; set; } = string.Empty;
        public string SupplierName { get; set; } = string.Empty;
        public decimal CurrentQuantity { get; set; }
        public string PhysicalState { get; set; } = string.Empty;
        public DateTime ExpiryDate { get; set; }
    }
}
