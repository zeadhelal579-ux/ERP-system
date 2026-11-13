using ERP_system.Common;
using ERP_system.Data;
using ERP_system.DTOs.Production;
using ERP_system.DTOs.Shipping;
using ERP_system.Exceptions;
using ERP_system.Models;
using ERP_system.Repositories;
using ERP_system.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ERP_system.Services
{
    public class ShippingService : IShippingService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;
        private readonly ILogger<ShippingService> _logger;
        private readonly AppDbContext _db;

        public ShippingService(IUnitOfWork unitOfWork, IAuditService auditService, ILogger<ShippingService> logger, AppDbContext db)
        {
            _unitOfWork = unitOfWork;
            _auditService = auditService;
            _logger = logger;
            _db = db;
        }

        public async Task<ShipmentResponseDto> CreateShipmentAsync(CreateShipmentDto request, int userId)
        {
            var batch = await _unitOfWork.ProductionBatches.GetByIdAsync(request.BatchId);

            if (batch == null)
                throw NotFoundException.For("التشغيلة", "BATCH_NOT_FOUND");

            if (batch.CurrentState != "Ready_For_Shipping")
                throw new InvalidStateTransitionException(
                    $"لا يمكن شحن التشغيلة وهي في حالة '{batch.CurrentState}'");

            if (batch.IsHeld || batch.ExpiryDate?.Date < DateTime.UtcNow.Date)
                throw new ConflictException("التشغيلة محجوزة أو منتهية الصلاحية", "BATCH_UNAVAILABLE");

            if (await _db.MaterialIssues.AnyAsync(issue => issue.BatchId == batch.Id) ||
                await _db.FinishedPallets.AnyAsync(pallet => pallet.BatchId == batch.Id))
                throw new ConflictException("استخدم مسح البالات لشحن التشغيلة", "PALLET_DISPATCH_REQUIRED");

            var shipment = new ShippingLog
            {
                BatchId = request.BatchId,
                ClientName = request.ClientName,
                DriverName = request.DriverName,
                TruckPlates = request.TruckPlates,
                ShippedBy = userId,
                // Finding #12: توحيد كل توقيتات النظام على UTC
                ShippingDate = DateTime.UtcNow
            };

            await _unitOfWork.ShippingLogs.AddAsync(shipment);

            batch.CurrentState = "Shipped";
            _unitOfWork.ProductionBatches.Update(batch);

            await _auditService.LogAsync(userId, "Batch_Shipped", "Batch", batch.BatchNumber,
                $"للعميل: {request.ClientName}");

            try
            {
                await _unitOfWork.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex, "تعارض تزامن أثناء شحن التشغيلة {BatchNumber}", batch.BatchNumber);
                throw new ConflictException(
                    "التشغيلة اتعدّلت من عملية تانية في نفس اللحظة، من فضلك أعد المحاولة",
                    "CONCURRENCY_CONFLICT");
            }

            _logger.LogInformation("تم شحن التشغيلة {BatchNumber} للعميل {ClientName}", batch.BatchNumber, request.ClientName);

            return MapToDto(shipment, batch.BatchNumber);
        }

        public async Task<IEnumerable<BatchSummaryDto>> GetReadyForShippingAsync()
        {
            var batches = await _unitOfWork.ProductionBatches.FindAsync(b => b.CurrentState == "Ready_For_Shipping" && !b.IsHeld &&
                !b.MaterialIssues.Any() && (b.ExpiryDate == null || b.ExpiryDate.Value.Date >= DateTime.UtcNow.Date));
            return batches.Select(b => new BatchSummaryDto
            {
                Id = b.Id,
                BatchNumber = b.BatchNumber,
                ProductId = b.ProductId,
                ProducedQty = b.ProducedQty,
                ExpiryDate = b.ExpiryDate,
                CreatedAt = b.CreatedAt
            });
        }

        public async Task<PagedResult<ShipmentResponseDto>> GetAllShipmentsAsync(PaginationParams pagination)
        {
            // القسم 4.9: بنستخدم Include هنا عشان نجيب رقم التشغيلة في نفس الاستعلام
            // بدل ما نضطر نعمل round-trip منفصل لكل شحنة (N+1).
            var query = _unitOfWork.ShippingLogs.Query().Include(s => s.ProductionBatch);

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderByDescending(s => s.Id)
                .Skip(pagination.Skip)
                .Take(pagination.PageSize)
                .ToListAsync();

            return new PagedResult<ShipmentResponseDto>
            {
                Items = items.Select(s => MapToDto(s, s.ProductionBatch?.BatchNumber ?? "")),
                Page = pagination.Page,
                PageSize = pagination.PageSize,
                TotalCount = totalCount
            };
        }

        private static ShipmentResponseDto MapToDto(ShippingLog s, string batchNumber)
        {
            return new ShipmentResponseDto
            {
                Id = s.Id,
                BatchId = s.BatchId,
                BatchNumber = batchNumber,
                ClientName = s.ClientName,
                DriverName = s.DriverName,
                TruckPlates = s.TruckPlates,
                ShippingDate = s.ShippingDate
            };
        }
    }
}
