using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP_system.Models
{
    public class LabResult
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Type { get; set; } = string.Empty; // IncomingMaterial, FinishedBatch

        public int? BatchId { get; set; }

        [ForeignKey("BatchId")]
        public ProductionBatch? ProductionBatch { get; set; }

        public int? RawMaterialId { get; set; }

        [ForeignKey("RawMaterialId")]
        public RawMaterial? RawMaterial { get; set; }

        [Required]
        public string ResultDetails { get; set; } = string.Empty;

        public bool IsPassed { get; set; }

        public string? Notes { get; set; }

        [Required]
        public int TestedBy { get; set; }

        [ForeignKey("TestedBy")]
        public User? TestedByUser { get; set; }

        // Finding #12: توحيد كل توقيتات النظام على UTC بدل التوقيت المحلي للسيرفر
        public DateTime TestedAt { get; set; } = DateTime.UtcNow;
    }
}
