using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP_system.Models
{
    public class RawMaterial
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string MaterialName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string SupplierName { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal CurrentQuantity { get; set; }

        [Required]
        [StringLength(50)]
        public string PhysicalState { get; set; } = string.Empty; // Liquid, Solid, Frozen, Powder (راجع AddRawMaterialValidator)

        [Required]
        public DateTime ExpiryDate { get; set; }

        public bool IsQCApproved { get; set; } = false;

        // Finding #5 (أولوية عالية): Concurrency Token — يمنع سيناريو "طلبين يصرفوا نفس
        // الخامة في نفس اللحظة" (Race Condition) من إنتاج كمية سالبة أو صرف أكبر من المتاح.
        [MaxLength(50)]
        public string? LotCode { get; set; }
        [MaxLength(100)]
        public string? SupplierLot { get; set; }
        [MaxLength(10)]
        public string Unit { get; set; } = "kg";

        [Timestamp]
        public byte[]? RowVersion { get; set; }

        // Navigation Property خالصة لحل N+1 في شاشة التتبع — معتمدة على FK موجود بالفعل
        // (LabResult.RawMaterialId)، مفيهاش عمود جديد فمحتاجاش Migration.
        public ICollection<LabResult>? LabResults { get; set; }
    }
}
