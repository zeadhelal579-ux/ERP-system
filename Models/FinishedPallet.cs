// Links a finished pallet to its production batch and optional dispatch.
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP_system.Models;

public class FinishedPallet
{
    public int Id { get; set; }
    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;
    public int BatchId { get; set; }
    public ProductionBatch Batch { get; set; } = null!;
    [Column(TypeName = "decimal(18,2)")]
    public decimal Quantity { get; set; }
    public int? DispatchId { get; set; }
    public PalletDispatch? Dispatch { get; set; }
    [Timestamp]
    public byte[]? RowVersion { get; set; }
}
