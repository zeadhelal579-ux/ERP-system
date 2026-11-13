namespace ERP_system.DTOs.Lab
{
    public class LabResultResponseDto
    {
        public int Id { get; set; }
        public string InspectionType { get; set; } = string.Empty;
        public int? BatchId { get; set; }
        public int? RawMaterialId { get; set; }
        public string ResultDetails { get; set; } = string.Empty;
        public bool IsPassed { get; set; }
        public string? Notes { get; set; }
        public DateTime TestedAt { get; set; }
    }
}
