using ERP_system.Models;

namespace ERP_system.DTOs.Production
{
    /// <summary>
    /// تعيين مشترك (ProductionBatch -> BatchResponseDto) مُستخدَم في أكتر من Service
    /// (Production، Manager، Scrap) — بدل تكرار نفس دالة MapToDto حرفيًا في كل واحدة منهم.
    /// </summary>
    public static class BatchMapper
    {
        public static BatchResponseDto ToDto(ProductionBatch b)
        {
            return new BatchResponseDto
            {
                Id = b.Id,
                BatchNumber = b.BatchNumber,
                ProductId = b.ProductId,
                RawMaterialId = b.RawMaterialId,
                IssuedMaterialQty = b.IssuedMaterialQty,
                ProducedQty = b.ProducedQty,
                ReturnedMaterialQty = b.ReturnedMaterialQty,
                ScrapQty = b.ScrapQty,
                CurrentState = b.CurrentState,
                IsReworked = b.IsReworked,
                ExpiryDate = b.ExpiryDate,
                CreatedAt = b.CreatedAt
            };
        }
    }
}
