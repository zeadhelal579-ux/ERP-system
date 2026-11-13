// Defines the lot, pallet and recall contracts used by the bottling workflow.
using System.ComponentModel.DataAnnotations;

namespace ERP_system.DTOs.Production;

public record AvailableIssue(int Id, int MaterialId, string MaterialName, string? LotCode, string Unit, decimal Quantity);
public record ProductOption(int Id, string ProductName, string? Sku);
public record BatchHoldRequest([Required] bool? IsHeld, [Required, StringLength(500)] string Reason);
public record PackPalletRequest([Required, StringLength(50)] string Code, decimal Quantity);
public record DispatchPalletsRequest(
    Guid RequestId,
    [Required, StringLength(100)] string ClientName,
    [Required, StringLength(50)] string TruckPlates,
    [Required, MinLength(1), MaxLength(100)] string[] PalletCodes);
public record PalletSummary(int Id, string Code, int BatchId, string BatchNumber, decimal Quantity, string State,
    string? ClientName, DateTime? ShippedAt);
public record RecallBatch(int Id, string BatchNumber, string State, bool IsHeld, decimal ProducedQuantity,
    string? LegacyClient, DateTime? LegacyShippedAt, IReadOnlyList<PalletSummary> Pallets);
public record RecallReport(int MaterialId, string? LotCode, string? SupplierLot, string MaterialName, string SupplierName,
    string Unit, decimal StockQuantity, IReadOnlyList<RecallBatch> Batches);
