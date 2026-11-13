using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP_system.Models
{
    public class AuditLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User? User { get; set; }

        [Required]
        [StringLength(100)]
        public string Action { get; set; } = string.Empty;

        public string? Details { get; set; }

        // Finding #12: توحيد كل توقيتات النظام على UTC بدل التوقيت المحلي للسيرفر
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
