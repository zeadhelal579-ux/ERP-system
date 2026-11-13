using System.ComponentModel.DataAnnotations;

namespace ERP_system.DTOs.Store
{
    public class RecordMaterialWasteDto
    {
        [Required]
        public int RawMaterialId { get; set; }

        [Required]
        public decimal WastedQuantity { get; set; }

        [Required]
        public string Reason { get; set; } = string.Empty; // مثلاً: تلف بسبب سوء التخزين
    }
}
