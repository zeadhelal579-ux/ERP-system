using System.ComponentModel.DataAnnotations;

namespace ERP_system.Models
{
    public class Product
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string ProductName { get; set; } = string.Empty;

        [StringLength(50)]
        public string? SKU { get; set; }
    }
}
