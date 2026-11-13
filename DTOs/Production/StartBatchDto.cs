using System.ComponentModel.DataAnnotations;

namespace ERP_system.DTOs.Production
{
    public class StartBatchDto
    {
        public Guid? RequestId { get; set; }
        public int? PrimaryIssueId { get; set; }
        [MaxLength(30)]
        public int[] AdditionalIssueIds { get; set; } = [];

        [Required]
        public int ProductId { get; set; }

        [Required]
        public int RawMaterialId { get; set; }

        [Required]
        public decimal IssuedMaterialQty { get; set; } // الكمية اللي المخزن صرفها فعلياً لخط الإنتاج
    }
}
