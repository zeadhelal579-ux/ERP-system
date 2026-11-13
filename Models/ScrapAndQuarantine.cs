using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP_system.Models
{
    public class ScrapAndQuarantine
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string SourceType { get; set; } = string.Empty; // RawMaterial, ProductionLine, ManagerRejection

        [Required]
        public int ReferenceId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Quantity { get; set; }

        [Required]
        public string ScrapReason { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string FinalDecision { get; set; } = "Pending"; // Pending, Sent_To_Rework, Destroyed, Reworked

        public int? DecisionBy { get; set; }

        [ForeignKey("DecisionBy")]
        public User? DecisionByUser { get; set; }

        // Finding #12: توحيد كل توقيتات النظام على UTC بدل التوقيت المحلي للسيرفر
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
