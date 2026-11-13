// Records the destination and operator for a pallet dispatch.
using System.ComponentModel.DataAnnotations;

namespace ERP_system.Models;

public class PalletDispatch
{
    public int Id { get; set; }
    public Guid RequestId { get; set; }
    [MaxLength(100)]
    public string ClientName { get; set; } = string.Empty;
    [MaxLength(50)]
    public string TruckPlates { get; set; } = string.Empty;
    public int ShippedBy { get; set; }
    public User Operator { get; set; } = null!;
    public DateTime ShippedAt { get; set; }
}
