using System.ComponentModel.DataAnnotations;

namespace ERP_system.DTOs.Scrap
{
    public class ScrapDecisionDto
    {
        [Required]
        public int ScrapEntryId { get; set; }

        [Required]
        public string Decision { get; set; } = string.Empty; // "Destroy" أو "RequestRework"
    }
}
