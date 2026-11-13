// Enforces pallet packing, quality holds and dispatch against the current batch state.
using ERP_system.Data;
using ERP_system.DTOs.Production;
using ERP_system.Exceptions;
using ERP_system.Models;
using ERP_system.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ERP_system.Services;

public class BottlingService(AppDbContext db, IAuditService audit)
{
    public async Task<IReadOnlyList<ProductOption>> GetProductsAsync(CancellationToken ct) =>
        await db.Products.AsNoTracking().OrderBy(product => product.ProductName)
            .Select(product => new ProductOption(product.Id, product.ProductName, product.SKU)).ToListAsync(ct);

    public async Task<IReadOnlyList<AvailableIssue>> GetAvailableIssuesAsync(CancellationToken ct)
    {
        var today = DateTime.UtcNow.Date;
        return await db.MaterialIssues.AsNoTracking()
            .Where(issue => issue.BatchId == null && issue.Material.IsQCApproved && issue.Material.ExpiryDate >= today)
            .OrderBy(issue => issue.Material.ExpiryDate).ThenBy(issue => issue.Id)
            .Select(issue => new AvailableIssue(issue.Id, issue.RawMaterialId, issue.Material.MaterialName,
                issue.Material.LotCode, issue.Material.Unit, issue.Quantity)).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<BatchSummaryDto>> GetPackableBatchesAsync(CancellationToken ct)
    {
        var today = DateTime.UtcNow.Date;
        return await db.ProductionBatches.AsNoTracking()
            .Where(batch => batch.CurrentState == "Ready_For_Shipping" && !batch.IsHeld &&
                (batch.ExpiryDate == null || batch.ExpiryDate >= today) && batch.ProducedQty > 0 && batch.MaterialIssues.Any())
            .OrderByDescending(batch => batch.Id).Take(200)
            .Select(batch => new BatchSummaryDto
            {
                Id = batch.Id,
                BatchNumber = batch.BatchNumber,
                ProductId = batch.ProductId,
                ProducedQty = batch.ProducedQty,
                CurrentState = batch.CurrentState,
                ExpiryDate = batch.ExpiryDate,
                CreatedAt = batch.CreatedAt
            }).ToListAsync(ct);
    }

    public async Task<bool> SetHoldAsync(int batchId, BatchHoldRequest request, int userId, CancellationToken ct)
    {
        var batch = await GetBatchAsync(batchId, ct);
        if (batch.CurrentState is "Destroyed" or "Shipped")
            throw new ConflictException("التشغيلة مقفلة. استخدم بيانات التتبع لو خرجت الشحنة بالفعل", "BATCH_CLOSED");
        var isHeld = request.IsHeld ?? throw new BusinessRuleException("HOLD_DECISION_REQUIRED", "حدد قرار الحجز");
        if (batch.IsHeld == isHeld)
            return batch.IsHeld;

        batch.IsHeld = isHeld;
        await audit.LogAsync(userId, isHeld ? "Batch_Held" : "Batch_Hold_Released",
            "Batch", batch.BatchNumber, request.Reason.Trim());
        await db.SaveChangesAsync(ct);
        return batch.IsHeld;
    }

    public async Task<PalletSummary> PackAsync(int batchId, PackPalletRequest request, int userId, CancellationToken ct)
    {
        var batch = await GetBatchAsync(batchId, ct);
        var code = NormalizeCode(request.Code);
        var existing = await db.FinishedPallets.Include(pallet => pallet.Dispatch)
            .SingleOrDefaultAsync(pallet => pallet.Code == code, ct);
        if (existing is not null)
        {
            if (existing.BatchId != batchId || existing.Quantity != request.Quantity)
                throw new ConflictException("باركود البالة مستخدم بالفعل", "PALLET_CODE_CONFLICT");
            return MapPallet(existing);
        }

        EnsureDispatchable(batch);
        if (!await db.MaterialIssues.AnyAsync(issue => issue.BatchId == batch.Id, ct))
            throw new ConflictException("التشغيلة القديمة تستخدم مسار الشحن الكامل", "LEGACY_BATCH");

        var packed = await db.FinishedPallets.Where(pallet => pallet.BatchId == batchId)
            .SumAsync(pallet => pallet.Quantity, ct);
        if (request.Quantity <= 0 || decimal.Round(request.Quantity, 2) != request.Quantity ||
            request.Quantity > batch.ProducedQty - packed)
            throw new BusinessRuleException("INVALID_PALLET_QUANTITY", "كمية البالة لازم تكون ضمن الكمية المتبقية وبحد أقصى منزلتين عشريتين");

        var pallet = new FinishedPallet { Batch = batch, Code = code, Quantity = request.Quantity };
        db.FinishedPallets.Add(pallet);
        TouchBatch(batch);
        await audit.LogAsync(userId, "Pallet_Packed", "Batch", batch.BatchNumber, code);
        await db.SaveChangesAsync(ct);
        return MapPallet(pallet);
    }

    public async Task<IReadOnlyList<PalletSummary>> GetPalletsAsync(int? batchId, CancellationToken ct)
    {
        var query = db.FinishedPallets.AsNoTracking().Include(pallet => pallet.Batch)
            .Include(pallet => pallet.Dispatch).AsQueryable();
        if (batchId.HasValue)
            query = query.Where(pallet => pallet.BatchId == batchId);
        else
            query = query.Where(pallet => pallet.DispatchId == null);
        return (await query.OrderByDescending(pallet => pallet.Id).Take(200).ToListAsync(ct))
            .Select(MapPallet).ToList();
    }

    public async Task<IReadOnlyList<PalletSummary>> DispatchAsync(DispatchPalletsRequest request, int userId, CancellationToken ct)
    {
        if (request.RequestId == Guid.Empty)
            throw new BusinessRuleException("REQUEST_ID_REQUIRED", "معرّف الطلب مطلوب");
        var codes = request.PalletCodes.Select(NormalizeCode).Distinct().ToArray();
        if (codes.Length != request.PalletCodes.Length)
            throw new BusinessRuleException("DUPLICATE_PALLET", "في باركود بالة مكرر في الطلب");

        var pallets = await db.FinishedPallets.Include(pallet => pallet.Batch).Include(pallet => pallet.Dispatch)
            .Where(pallet => codes.Contains(pallet.Code)).OrderBy(pallet => pallet.Id).ToListAsync(ct);
        if (pallets.Count != codes.Length)
            throw NotFoundException.For("بالة من القائمة", "PALLET_NOT_FOUND");

        var existing = await db.PalletDispatches.SingleOrDefaultAsync(dispatch => dispatch.RequestId == request.RequestId, ct);
        if (existing is not null)
        {
            var count = await db.FinishedPallets.CountAsync(pallet => pallet.DispatchId == existing.Id, ct);
            if (existing.ShippedBy != userId || existing.ClientName != request.ClientName.Trim() ||
                existing.TruckPlates != request.TruckPlates.Trim() || count != codes.Length ||
                pallets.Any(pallet => pallet.DispatchId != existing.Id))
                throw new ConflictException("معرّف الطلب مرتبط بشحنة مختلفة", "REQUEST_ID_CONFLICT");
            return pallets.Select(MapPallet).ToList();
        }

        if (pallets.Any(pallet => pallet.DispatchId != null))
            throw new ConflictException("إحدى البالات اتشحنت بالفعل", "PALLET_ALREADY_SHIPPED");

        var dispatch = new PalletDispatch
        {
            RequestId = request.RequestId,
            ClientName = request.ClientName.Trim(),
            TruckPlates = request.TruckPlates.Trim(),
            ShippedBy = userId,
            ShippedAt = DateTime.UtcNow
        };
        foreach (var group in pallets.GroupBy(pallet => pallet.Batch))
        {
            var batch = group.Key;
            EnsureDispatchable(batch);
            var shipped = await db.FinishedPallets.Where(pallet => pallet.BatchId == batch.Id && pallet.DispatchId != null)
                .SumAsync(pallet => pallet.Quantity, ct);
            if (shipped + group.Sum(pallet => pallet.Quantity) == batch.ProducedQty)
                batch.CurrentState = "Shipped";
            TouchBatch(batch);
            await audit.LogAsync(userId, "Pallets_Dispatched", "Batch", batch.BatchNumber,
                string.Join(", ", group.Select(pallet => pallet.Code)));
        }
        foreach (var pallet in pallets)
            pallet.Dispatch = dispatch;

        await db.SaveChangesAsync(ct);
        return pallets.Select(MapPallet).ToList();
    }

    public async Task<RecallReport> GetRecallAsync(string lotCode, CancellationToken ct)
    {
        var code = NormalizeCode(lotCode);
        var lot = await db.RawMaterials.AsNoTracking().SingleOrDefaultAsync(material => material.LotCode == code, ct)
            ?? throw NotFoundException.For("دفعة الخامة", "LOT_NOT_FOUND");

        var batches = await db.ProductionBatches.AsNoTracking().Include(batch => batch.Shipment)
            .Where(batch => batch.RawMaterialId == lot.Id || batch.MaterialIssues.Any(issue => issue.RawMaterialId == lot.Id))
            .OrderBy(batch => batch.Id).ToListAsync(ct);
        var ids = batches.Select(batch => batch.Id).ToArray();
        var pallets = await db.FinishedPallets.AsNoTracking().Include(pallet => pallet.Batch).Include(pallet => pallet.Dispatch)
            .Where(pallet => ids.Contains(pallet.BatchId)).OrderBy(pallet => pallet.Id).ToListAsync(ct);
        var byBatch = pallets.ToLookup(pallet => pallet.BatchId);

        return new RecallReport(lot.Id, lot.LotCode, lot.SupplierLot, lot.MaterialName, lot.SupplierName,
            lot.Unit, lot.CurrentQuantity, batches.Select(batch => new RecallBatch(batch.Id, batch.BatchNumber,
                batch.CurrentState, batch.IsHeld, batch.ProducedQty, batch.Shipment?.ClientName, batch.Shipment?.ShippingDate,
                byBatch[batch.Id].Select(MapPallet).ToList())).ToList());
    }

    private async Task<ProductionBatch> GetBatchAsync(int batchId, CancellationToken ct) =>
        await db.ProductionBatches.FindAsync([batchId], ct)
            ?? throw NotFoundException.For("التشغيلة", "BATCH_NOT_FOUND");

    private static void EnsureDispatchable(ProductionBatch batch)
    {
        if (batch.IsHeld || batch.CurrentState != "Ready_For_Shipping" || batch.ExpiryDate?.Date < DateTime.UtcNow.Date)
            throw new ConflictException("التشغيلة محجوزة أو غير معتمدة أو منتهية الصلاحية", "BATCH_UNAVAILABLE");
    }

    private void TouchBatch(ProductionBatch batch)
    {
        // Advancing the row version serializes packing and dispatch with concurrent quality holds.
        db.Entry(batch).Property(item => item.CurrentState).IsModified = true;
    }

    private static string NormalizeCode(string? value)
    {
        var code = value?.Trim().ToUpperInvariant();
        if (string.IsNullOrEmpty(code) || code.Length > 50 || code.Any(ch => !char.IsAsciiLetterOrDigit(ch) && ch != '-'))
            throw new BusinessRuleException("INVALID_BARCODE", "الباركود لازم يكون حروف إنجليزية أو أرقام أو شرطات");
        return code;
    }

    private static PalletSummary MapPallet(FinishedPallet pallet) =>
        new(pallet.Id, pallet.Code, pallet.BatchId, pallet.Batch.BatchNumber, pallet.Quantity,
            pallet.Dispatch is not null ? "Shipped" : pallet.Batch.IsHeld ? "Held" : "In_Stock",
            pallet.Dispatch?.ClientName, pallet.Dispatch?.ShippedAt);
}
