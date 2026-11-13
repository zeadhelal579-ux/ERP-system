using System.ComponentModel.DataAnnotations;

namespace ERP_system.DTOs.Manager
{
    public class ManagerDecisionDto
    {
        [Required]
        public int BatchId { get; set; }

        [Required]
        public bool Approve { get; set; } // true = موافقة، false = رفض

        public string? Reason { get; set; } // سبب الرفض (اختياري في حالة الموافقة)
    }
}
