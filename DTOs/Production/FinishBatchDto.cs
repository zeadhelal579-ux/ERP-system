using System.ComponentModel.DataAnnotations;

namespace ERP_system.DTOs.Production
{
    public class FinishBatchDto
    {
        [Required]
        public int BatchId { get; set; }

        [Required]
        public decimal ProducedQty { get; set; }

        [Required]
        public decimal ReturnedMaterialQty { get; set; }

        [Required]
        public DateTime ExpiryDate { get; set; }
    }
}
