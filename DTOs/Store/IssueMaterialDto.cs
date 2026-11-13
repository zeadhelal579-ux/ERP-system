using System.ComponentModel.DataAnnotations;

namespace ERP_system.DTOs.Store
{
    public class IssueMaterialDto
    {
        public Guid? RequestId { get; set; }

        [Required]
        public int RawMaterialId { get; set; }

        [Required]
        public decimal QuantityToIssue { get; set; }
    }
}
