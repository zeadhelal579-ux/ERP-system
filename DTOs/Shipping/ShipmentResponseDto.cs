namespace ERP_system.DTOs.Shipping
{
    public class ShipmentResponseDto
    {
        public int Id { get; set; }
        public int BatchId { get; set; }

        // إضافة متعمدة (مش موجودة في الأصل): من غيرها كان إصلاح مشكلة N+1 في
        // ShippingService (عبر Include على ProductionBatch) هيبقى تحسين أداء
        // من غير أي فايدة ملموسة للفرونت — هنا القيمة الفعلية بتوصله.
        public string BatchNumber { get; set; } = string.Empty;

        public string ClientName { get; set; } = string.Empty;
        public string DriverName { get; set; } = string.Empty;
        public string TruckPlates { get; set; } = string.Empty;
        public DateTime ShippingDate { get; set; }
    }
}
