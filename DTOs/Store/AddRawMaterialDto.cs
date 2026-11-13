using System.ComponentModel.DataAnnotations;

namespace ERP_system.DTOs.Store
{
    public class AddRawMaterialDto
    {
        [Required]
        public string MaterialName { get; set; } = string.Empty;

        [Required]
        public string SupplierName { get; set; } = string.Empty;

        [Required]
        public decimal Quantity { get; set; }

        [Required]
        public string PhysicalState { get; set; } = string.Empty; // Liquid, Solid, Frozen, Powder (نفس تعليق RawMaterial.cs — كان ناقص هنا، اتصلح في جولة الاختبار)

        [StringLength(100)]
        public string? SupplierLot { get; set; }

        [RegularExpression("kg|L|pcs")]
        public string Unit { get; set; } = "kg";

        [Required]
        public DateTime ExpiryDate { get; set; }
    }
}
