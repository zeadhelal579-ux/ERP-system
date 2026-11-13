using ERP_system.DTOs.Reports;
using ERP_system.Exceptions;
using ERP_system.Models;
using ERP_system.Repositories;
using ERP_system.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ERP_system.Services
{
    public class ReportsService : IReportsService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ReportsService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<TraceabilityResponseDto> GetBatchTraceabilityAsync(int batchId)
        {
            var batch = await _unitOfWork.ProductionBatches.GetByIdAsync(batchId);
            if (batch == null)
                throw NotFoundException.For("التشغيلة", "BATCH_NOT_FOUND");

            return await BuildTraceabilityDto(batch);
        }

        public async Task<TraceabilityResponseDto> GetBatchTraceabilityByNumberAsync(string batchNumber)
        {
            var batches = await _unitOfWork.ProductionBatches.FindAsync(b => b.BatchNumber == batchNumber);
            var batch = batches.FirstOrDefault();
            if (batch == null)
                throw NotFoundException.For("التشغيلة", "BATCH_NOT_FOUND");

            return await BuildTraceabilityDto(batch);
        }

        private async Task<TraceabilityResponseDto> BuildTraceabilityDto(ProductionBatch batch)
        {
            // ==========================================================================
            // القسم 4.9 (حل N+1): الكود الأصلي كان بيجيب Material وCreator وLabResults
            // وShipment كل واحد في استعلام منفصل (4 round-trips على الأقل). دلوقتي بنجيبهم
            // كلهم في استعلام واحد بـ JOIN حقيقي عبر GetByIdAsync overload اللي بيقبل Include
            // (Repositories/GenericRepository.cs)، معتمدين على الـ Navigation Properties
            // الجديدة اللي اتضافت على ProductionBatch (LabResults, Shipment).
            // ==========================================================================
            var fullBatch = await _unitOfWork.ProductionBatches.GetByIdAsync(
                batch.Id,
                b => b.RawMaterial!,
                b => b.CreatedByUser!,
                b => b.LabResults!,
                b => b.Shipment!) ?? batch;

            var labSummary = (fullBatch.LabResults ?? Enumerable.Empty<LabResult>())
                .OrderBy(l => l.TestedAt)
                .Select(l => $"{(l.IsPassed ? "نجح" : "سقط")} - {l.TestedAt:yyyy-MM-dd}")
                .ToList();

            // ScrapAndQuarantine.ReferenceId مرجع متعدد الأشكال بدون FK حقيقي (القسم 3.3-د)،
            // فمينفعش يتحول لـ Include من غير تعديل الـ Schema (مؤجّل لمرحلة لاحقة —
            // شوف الريبورت). ده استعلام إضافي واحد بس، مش N استعلامات زي الوضع القديم.
            var scrapEntries = await _unitOfWork.ScrapAndQuarantines.FindAsync(
                s => s.ReferenceId == fullBatch.Id && s.SourceType == "ManagerRejection");
            var scrapEntry = scrapEntries.FirstOrDefault();
            string? scrapSummary = scrapEntry == null ? null :
                $"الحالة: {scrapEntry.FinalDecision} - السبب: {scrapEntry.ScrapReason}";

            string? shippingSummary = fullBatch.Shipment == null ? null :
                $"شُحن للعميل {fullBatch.Shipment.ClientName} بتاريخ {fullBatch.Shipment.ShippingDate:yyyy-MM-dd}";

            // ملاحظة: الحالتين "Destroyed" و "Rework_In_Progress" اتضافوا هنا كنتيجة مباشرة
            // لإصلاح Finding #6 — دلوقتي التشغيلة فعلاً بتوصل للحالتين دول بعد رفض المدير،
            // فلازم برضو تفضل تظهر "تم الرفض من المدير" في تاريخها بدل ما ترجع null فجأة.
            string? managerSummary = fullBatch.CurrentState switch
            {
                "Ready_For_Shipping" or "Shipped" => "تمت الموافقة من المدير",
                "Quarantine" or "Destroyed" or "Rework_In_Progress" => "تم الرفض من المدير",
                _ => null
            };

            return new TraceabilityResponseDto
            {
                BatchId = fullBatch.Id,
                BatchNumber = fullBatch.BatchNumber,
                CurrentState = fullBatch.CurrentState,
                IsReworked = fullBatch.IsReworked,
                RawMaterialName = fullBatch.RawMaterial?.MaterialName,
                IssuedMaterialQty = fullBatch.IssuedMaterialQty,
                ProducedQty = fullBatch.ProducedQty,
                ReturnedMaterialQty = fullBatch.ReturnedMaterialQty,
                ScrapQty = fullBatch.ScrapQty,
                ExpiryDate = fullBatch.ExpiryDate,
                CreatedAt = fullBatch.CreatedAt,
                CreatedByUser = fullBatch.CreatedByUser?.FullName,
                LabResultsSummary = labSummary,
                ManagerDecisionSummary = managerSummary,
                ShippingSummary = shippingSummary,
                ScrapSummary = scrapSummary
            };
        }

        public async Task<DashboardStatsDto> GetDashboardStatsAsync()
        {
            // ==========================================================================
            // القسم 4.9 (تجميع لوحة المعلومات): الكود الأصلي كان بيجيب *كل* التشغيلات
            // و*كل* سطور الهالك للذاكرة (GetAllAsync) ثم يَعُدّ بـ LINQ محليًا — مشكلة أداء
            // حقيقية مع نمو البيانات. دلوقتي كل رقم بيتحسب بـ COUNT/SUM منفّذة فعليًا في
            // قاعدة البيانات نفسها (عبر Query() اللي بيرجّع IQueryable حقيقي)، من غير ما
            // نحمّل أي صف فعلي للذاكرة.
            // ==========================================================================
            var batchQuery = _unitOfWork.ProductionBatches.Query();

            var totalBatches = await batchQuery.CountAsync();
            var inProduction = await batchQuery.CountAsync(b => b.CurrentState == "In_Production");
            var waitingQc = await batchQuery.CountAsync(b => b.CurrentState == "Waiting_QC");
            var pendingApproval = await batchQuery.CountAsync(b => b.CurrentState == "QC_Passed");
            var shipped = await batchQuery.CountAsync(b => b.CurrentState == "Shipped");

            // Finding #6 تم إصلاحه في ScrapService: قرار "إعدام" دلوقتي بينقل التشغيلة
            // فعليًا لحالة "Destroyed"، فالعدّاد ده رجع يعكس التشغيلات المعزولة فعلاً وبس
            // (مش المُعدَمة اللي كانت عالقة غلط على "Quarantine" للأبد).
            var quarantine = await batchQuery.CountAsync(b => b.CurrentState == "Quarantine");

            var totalScrapQty = totalBatches == 0 ? 0 : await batchQuery.SumAsync(b => b.ScrapQty);
            var totalReworked = await batchQuery.CountAsync(b => b.IsReworked);

            var scrapQuery = _unitOfWork.ScrapAndQuarantines.Query();
            var anyDestroyed = await scrapQuery.AnyAsync(s => s.FinalDecision == "Destroyed");
            var totalDestroyedQty = anyDestroyed
                ? await scrapQuery.Where(s => s.FinalDecision == "Destroyed").SumAsync(s => s.Quantity)
                : 0;

            return new DashboardStatsDto
            {
                TotalBatches = totalBatches,
                InProductionCount = inProduction,
                WaitingQcCount = waitingQc,
                PendingApprovalCount = pendingApproval,
                ShippedCount = shipped,
                QuarantineCount = quarantine,

                TotalScrapQty = totalScrapQty,
                TotalDestroyedQty = totalDestroyedQty,
                TotalReworkedBatches = totalReworked
            };
        }
    }
}
