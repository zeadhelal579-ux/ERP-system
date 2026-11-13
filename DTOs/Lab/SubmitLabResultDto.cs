using System.ComponentModel.DataAnnotations;

namespace ERP_system.DTOs.Lab
{
    public class SubmitLabResultDto
    {
        [Required]
        public string InspectionType { get; set; } = string.Empty; // "IncomingMaterial" أو "FinishedBatch"

        public int? BatchId { get; set; }

        public int? RawMaterialId { get; set; }

        [Required]
        public string ResultDetails { get; set; } = string.Empty;

        [Required]
        public bool IsPassed { get; set; }

        public string? Notes { get; set; }
    }
}
