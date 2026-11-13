namespace ERP_system.DTOs.Production
{
    public class BatchResponseDto
    {
        public int Id { get; set; }
        public string BatchNumber { get; set; } = string.Empty;
        public int ProductId { get; set; }
        public int RawMaterialId { get; set; }
        public decimal IssuedMaterialQty { get; set; }
        public decimal ProducedQty { get; set; }
        public decimal ReturnedMaterialQty { get; set; }
        public decimal ScrapQty { get; set; }
        public string CurrentState { get; set; } = string.Empty;
        public bool IsReworked { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
