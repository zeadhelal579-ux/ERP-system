using ERP_system.Data;
using Microsoft.EntityFrameworkCore;

namespace ERP_system.BackgroundJobs
{
    /// <summary>
    /// القسم 4.8: "مجلد BackgroundJobs/ موجود بالفعل لكنه فارغ تمامًا... أول استخدام
    /// طبيعي له: تنبيه انتهاء الصلاحية". دلوقتي أول Job حقيقي فعليًا: فحص يومي لأي
    /// RawMaterial أو ProductionBatch قريب من ExpiryDate، وتسجيل تنبيه في اللوج
    /// (Structured Logging منفصل تمامًا عن AuditLog - القسم 4.7).
    ///
    /// ملاحظة مهمة: مفيش نظام إشعارات فعلي (إيميل/SMS) في المشروع لحد دلوقتي، فالـ Job
    /// ده بيسجّل تحذيرات في اللوج بس كخطوة أولى (v1) — التوسعة الطبيعية التالية هي ربط
    /// نفس البيانات دي بقناة إشعار فعلية (إيميل لمدير المخزن/الإنتاج) لما تتوفر.
    /// </summary>
    public class ExpiryAlertBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ExpiryAlertBackgroundService> _logger;
        private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(24);
        private const int WarningWindowDays = 7;

        public ExpiryAlertBackgroundService(IServiceScopeFactory scopeFactory, ILogger<ExpiryAlertBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("خدمة تنبيهات انتهاء الصلاحية بدأت العمل (فحص كل {Hours} ساعة)", CheckInterval.TotalHours);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckExpiringItemsAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    // مهم: أي خطأ هنا لازم يتسجّل بس مايوقفش الـ Background Service نفسه
                    // عن الاستمرار في الدورة الجاية - خدمة خلفية بتقع بالكامل أخطر من فحص واحد فشل.
                    _logger.LogError(ex, "خطأ أثناء فحص تنبيهات انتهاء الصلاحية");
                }

                try
                {
                    await Task.Delay(CheckInterval, stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    // إيقاف عادي للتطبيق - مش خطأ
                }
            }
        }

        private async Task CheckExpiringItemsAsync(CancellationToken stoppingToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var thresholdDate = DateTime.UtcNow.AddDays(WarningWindowDays);

            var expiringMaterials = await context.RawMaterials
                .Where(m => m.ExpiryDate <= thresholdDate && m.CurrentQuantity > 0)
                .Select(m => new { m.Id, m.MaterialName, m.ExpiryDate, m.CurrentQuantity })
                .ToListAsync(stoppingToken);

            foreach (var material in expiringMaterials)
            {
                _logger.LogWarning(
                    "تنبيه انتهاء صلاحية خامة: {MaterialName} (#{Id}) - الكمية المتبقية: {Quantity} - تنتهي في: {ExpiryDate:yyyy-MM-dd}",
                    material.MaterialName, material.Id, material.CurrentQuantity, material.ExpiryDate);
            }

            var expiringBatches = await context.ProductionBatches
                .Where(b => b.ExpiryDate != null && b.ExpiryDate <= thresholdDate
                            && b.CurrentState != "Shipped" && b.CurrentState != "Destroyed")
                .Select(b => new { b.Id, b.BatchNumber, b.ExpiryDate, b.CurrentState })
                .ToListAsync(stoppingToken);

            foreach (var batch in expiringBatches)
            {
                _logger.LogWarning(
                    "تنبيه انتهاء صلاحية تشغيلة: {BatchNumber} (#{Id}) - الحالة: {State} - تنتهي في: {ExpiryDate:yyyy-MM-dd}",
                    batch.BatchNumber, batch.Id, batch.CurrentState, batch.ExpiryDate);
            }

            if (expiringMaterials.Count == 0 && expiringBatches.Count == 0)
            {
                _logger.LogInformation("فحص انتهاء الصلاحية: مفيش خامات أو تشغيلات قريبة من الانتهاء دلوقتي");
            }
        }
    }
}
