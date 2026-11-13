using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP_system.Models
{
    public class ProductionBatch
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string BatchNumber { get; set; } = string.Empty;

        [Required]
        public int ProductId { get; set; }

        [ForeignKey("ProductId")]
        public Product? Product { get; set; }

        [Required]
        public int RawMaterialId { get; set; }

        [ForeignKey("RawMaterialId")]
        public RawMaterial? RawMaterial { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal IssuedMaterialQty { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ProducedQty { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ReturnedMaterialQty { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ScrapQty { get; set; }

        [Required]
        [StringLength(50)]
        public string CurrentState { get; set; } = "In_Production";

        public bool IsReworked { get; set; } = false;

        public DateTime? ExpiryDate { get; set; }

        [Required]
        public int CreatedBy { get; set; }

        [ForeignKey("CreatedBy")]
        public User? CreatedByUser { get; set; }

        // Finding #12: توحيد كل توقيتات النظام على UTC بدل التوقيت المحلي للسيرفر
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Finding #5 (أولوية عالية): Concurrency Token — يمنع تعارض تحديث نفس التشغيلة
        // من طلبين متزامنين (مثلاً إنهاء تشغيلة مرتين في نفس اللحظة). EF Core بيرفض
        // الحفظ تلقائيًا بـ DbUpdateConcurrencyException لو الـ RowVersion اتغيّر من
        // وقت ما القراءة حصلت، والـ Middleware بيحوّلها لـ ConflictException (409).
        public Guid? StartRequestId { get; set; }
        public bool IsHeld { get; set; }
        public ICollection<MaterialIssue> MaterialIssues { get; set; } = [];

        [Timestamp]
        public byte[]? RowVersion { get; set; }

        // === Navigation Properties المضافة لحل مشكلة N+1 (القسم 4.9) ===
        // دول Navigation Properties خالصة (Pure) معتمدة على الـ Foreign Keys الموجودة
        // بالفعل (LabResult.BatchId, ShippingLog.BatchId) — مفيهمش أي عمود جديد في القاعدة،
        // فمحتاجينش Migration عشان يتفعّلوا. EF Core بيكتشفهم تلقائيًا من العلاقة الموجودة.
        public ICollection<LabResult>? LabResults { get; set; }
        public ShippingLog? Shipment { get; set; }
    }
}
