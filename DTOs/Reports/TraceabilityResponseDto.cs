namespace ERP_system.DTOs.Reports
{
    public class TraceabilityResponseDto
    {
        public int BatchId { get; set; }
        public string BatchNumber { get; set; } = string.Empty;
        public string CurrentState { get; set; } = string.Empty;
        public bool IsReworked { get; set; }

        public string? RawMaterialName { get; set; }
        public decimal IssuedMaterialQty { get; set; }
        public decimal ProducedQty { get; set; }
        public decimal ReturnedMaterialQty { get; set; }
        public decimal ScrapQty { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? CreatedByUser { get; set; }

        public List<string> LabResultsSummary { get; set; } = new();
        public string? ManagerDecisionSummary { get; set; }
        public string? ShippingSummary { get; set; }
        public string? ScrapSummary { get; set; }
    }
}
