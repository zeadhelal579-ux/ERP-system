namespace ERP_system.DTOs.Store
{
    public class RawMaterialResponseDto
    {
        public string? LotCode { get; set; }
        public string? SupplierLot { get; set; }
        public string Unit { get; set; } = "kg";
        public int? IssueId { get; set; }
        public int Id { get; set; }
        public string MaterialName { get; set; } = string.Empty;
        public string SupplierName { get; set; } = string.Empty;
        public decimal CurrentQuantity { get; set; }
        public string PhysicalState { get; set; } = string.Empty;
        public DateTime ExpiryDate { get; set; }
        public bool IsQCApproved { get; set; }
    }
}
