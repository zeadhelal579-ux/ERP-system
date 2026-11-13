using ERP_system.Models;
using Microsoft.EntityFrameworkCore;

namespace ERP_system.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Role> Roles { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<RawMaterial> RawMaterials { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductionBatch> ProductionBatches { get; set; }
        public DbSet<LabResult> LabResults { get; set; }
        public DbSet<ScrapAndQuarantine> ScrapAndQuarantines { get; set; }
        public DbSet<ShippingLog> ShippingLogs { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }

        public DbSet<MaterialIssue> MaterialIssues => Set<MaterialIssue>();
        public DbSet<FinishedPallet> FinishedPallets => Set<FinishedPallet>();
        public DbSet<PalletDispatch> PalletDispatches => Set<PalletDispatch>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<RawMaterial>().HasIndex(material => material.LotCode).IsUnique();
            modelBuilder.Entity<ProductionBatch>().HasIndex(batch => batch.StartRequestId).IsUnique();
            modelBuilder.Entity<MaterialIssue>().HasIndex(issue => issue.RequestId).IsUnique();
            modelBuilder.Entity<MaterialIssue>().HasIndex(issue => new { issue.RawMaterialId, issue.BatchId });
            modelBuilder.Entity<MaterialIssue>().HasOne(issue => issue.Material).WithMany()
                .HasForeignKey(issue => issue.RawMaterialId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<MaterialIssue>().HasOne(issue => issue.Batch).WithMany(batch => batch.MaterialIssues)
                .HasForeignKey(issue => issue.BatchId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<MaterialIssue>().HasOne(issue => issue.Issuer).WithMany()
                .HasForeignKey(issue => issue.IssuedBy).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<MaterialIssue>().ToTable(table =>
                table.HasCheckConstraint("CK_MaterialIssues_Quantity", "[Quantity] > 0"));

            modelBuilder.Entity<FinishedPallet>().HasIndex(pallet => pallet.Code).IsUnique();
            modelBuilder.Entity<FinishedPallet>().HasOne(pallet => pallet.Batch).WithMany()
                .HasForeignKey(pallet => pallet.BatchId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<FinishedPallet>().HasOne(pallet => pallet.Dispatch).WithMany()
                .HasForeignKey(pallet => pallet.DispatchId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<FinishedPallet>().ToTable(table =>
                table.HasCheckConstraint("CK_FinishedPallets_Quantity", "[Quantity] > 0"));
            modelBuilder.Entity<PalletDispatch>().HasIndex(dispatch => dispatch.RequestId).IsUnique();
            modelBuilder.Entity<PalletDispatch>().HasOne(dispatch => dispatch.Operator).WithMany()
                .HasForeignKey(dispatch => dispatch.ShippedBy).OnDelete(DeleteBehavior.Restrict);

            // كسر الـ Cascade المزدوج: لو اتمسح اليوزر، سجلات الشحن متتاثرش تلقائي
            modelBuilder.Entity<ShippingLog>()
                .HasOne(s => s.ShippedByUser)
                .WithMany()
                .HasForeignKey(s => s.ShippedBy)
                .OnDelete(DeleteBehavior.Restrict);

            // ==========================================================================
            // اكتشاف جديد أثناء مراجعة الـ 10 لوب (مش من ضمن جدول 4.11 الأصلي ولا تقرير
            // الترحيل): كل الـ Foreign Keys الإجبارية التانية اللي بتشاور على User أو
            // Product أو RawMaterial كانت لسه على سلوك EF Core الافتراضي (Cascade)،
            // ده بيتعارض مباشرة مع مبدأين أساسيين موثقين في 02_Project_Vision.md:
            //   - "لا شيء يُنسى ولا شيء يُحذف" (AuditLogs لازم يفضل موجود للأبد، مهما حصل)
            //   - سجل التتبع الكامل هو "قلب النظام" (ProductionBatch/LabResult ما ينفعش
            //     يتمسحوا تلقائيًا بمجرد ما اليوزر أو المنتج أو الخامة المرتبطين بيهم يتمسحوا)
            // النظام دلوقتي مفيهوش أي Endpoint بيمسح User/Product/RawMaterial فعليًا،
            // فده مش Bug شغّال دلوقتي، لكنه فخ حقيقي لو اتضاف Endpoint "حذف مستخدم" لاحقًا
            // من غير الانتباه لده، أو لو حد نفّذ DELETE يدوي على قاعدة البيانات مباشرة.
            // الحل: نفس مبدأ الإصلاح الموجود فوق بالظبط (Restrict بدل Cascade) — لو حابب
            // "تمسح" مستخدم فعليًا استخدم User.IsActive = false (موجود بالفعل)، مش حذف حقيقي.
            // ملحوظة: ده تغيير Schema (تعديل قيود الـ FK) ومحتاج Migration جديدة — راجع
            // الريبورت المرفق لأمر dotnet ef الدقيق.
            // ==========================================================================
            modelBuilder.Entity<AuditLog>()
                .HasOne(a => a.User)
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<LabResult>()
                .HasOne(l => l.TestedByUser)
                .WithMany()
                .HasForeignKey(l => l.TestedBy)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProductionBatch>()
                .HasOne(b => b.CreatedByUser)
                .WithMany()
                .HasForeignKey(b => b.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProductionBatch>()
                .HasOne(b => b.Product)
                .WithMany()
                .HasForeignKey(b => b.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProductionBatch>()
                .HasOne(b => b.RawMaterial)
                .WithMany()
                .HasForeignKey(b => b.RawMaterialId)
                .OnDelete(DeleteBehavior.Restrict);

            // ==========================================================================
            // Finding #7 (القسم 4.11): فهارس Unique كانت غير موجودة على مستوى قاعدة البيانات،
            // والاعتماد كان فقط على تحقق منطقي بالكود (يفتح الباب لتعارض تحت التزامن/الضغط).
            // دلوقتي الحماية النهائية موجودة في القاعدة نفسها، مش بس في منطق الـ Service.
            // ملاحظة: ده تغيير في الـ Schema، فمحتاج Migration جديدة — شوف README.md
            // قسم "خطوات مطلوبة منك" لأمر dotnet ef الدقيق.
            // ==========================================================================
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            modelBuilder.Entity<ProductionBatch>()
                .HasIndex(b => b.BatchNumber)
                .IsUnique();

            // ==========================================================================
            // Navigation Properties الجديدة (ProductionBatch.LabResults / .Shipment،
            // RawMaterial.LabResults) بيتم اكتشافها تلقائيًا من الـ Foreign Keys الموجودة
            // بالفعل (LabResult.BatchId, LabResult.RawMaterialId, ShippingLog.BatchId) —
            // مفيش تكوين إضافي لازم هنا لأن كل علاقة منها Foreign Key وحيد بدون لبس.
            // ==========================================================================
        }
    }
}
