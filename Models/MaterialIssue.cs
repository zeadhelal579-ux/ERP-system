// Records a lot issue and the batch that consumed it.
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP_system.Models;

public class MaterialIssue
{
    public int Id { get; set; }
    public int RawMaterialId { get; set; }
    public RawMaterial Material { get; set; } = null!;
    public int? BatchId { get; set; }
    public ProductionBatch? Batch { get; set; }
    public Guid? RequestId { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal Quantity { get; set; }
    public int IssuedBy { get; set; }
    public User Issuer { get; set; } = null!;
    public DateTime IssuedAt { get; set; }
    [Timestamp]
    public byte[]? RowVersion { get; set; }
}
