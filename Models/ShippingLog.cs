using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP_system.Models
{
    public class ShippingLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int BatchId { get; set; }

        [ForeignKey("BatchId")]
        public ProductionBatch? ProductionBatch { get; set; }

        [Required]
        [StringLength(100)]
        public string ClientName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string DriverName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string TruckPlates { get; set; } = string.Empty;

        [Required]
        public int ShippedBy { get; set; }

        [ForeignKey("ShippedBy")]
        public User? ShippedByUser { get; set; }

        // Finding #12: توحيد كل توقيتات النظام على UTC بدل التوقيت المحلي للسيرفر
        public DateTime ShippingDate { get; set; } = DateTime.UtcNow;
    }
}
