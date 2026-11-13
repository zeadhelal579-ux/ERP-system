namespace ERP_system.DTOs.Reports
{
    public class DashboardStatsDto
    {
        public int TotalBatches { get; set; }
        public int InProductionCount { get; set; }
        public int WaitingQcCount { get; set; }
        public int PendingApprovalCount { get; set; }
        public int ShippedCount { get; set; }
        public int QuarantineCount { get; set; }

        public decimal TotalScrapQty { get; set; }
        public decimal TotalDestroyedQty { get; set; }
        public decimal TotalReworkedBatches { get; set; }
    }
}
