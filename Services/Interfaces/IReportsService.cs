using ERP_system.DTOs.Reports;

namespace ERP_system.Services.Interfaces
{
    public interface IReportsService
    {
        /// <summary>يطلق NotFoundException لو التشغيلة مش موجودة</summary>
        Task<TraceabilityResponseDto> GetBatchTraceabilityAsync(int batchId);

        /// <summary>يطلق NotFoundException لو مفيش تشغيلة بهذا الرقم</summary>
        Task<TraceabilityResponseDto> GetBatchTraceabilityByNumberAsync(string batchNumber);

        Task<DashboardStatsDto> GetDashboardStatsAsync();
    }
}
