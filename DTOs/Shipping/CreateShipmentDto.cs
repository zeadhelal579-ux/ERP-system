using System.ComponentModel.DataAnnotations;

namespace ERP_system.DTOs.Shipping
{
    public class CreateShipmentDto
    {
        [Required]
        public int BatchId { get; set; }

        [Required]
        public string ClientName { get; set; } = string.Empty;

        [Required]
        public string DriverName { get; set; } = string.Empty;

        [Required]
        public string TruckPlates { get; set; } = string.Empty;
    }
}
