using ERP_system.Models;

namespace ERP_system.Repositories
{
    public interface IUnitOfWork : IDisposable
    {
        IGenericRepository<Role> Roles { get; }
        IGenericRepository<User> Users { get; }
        IGenericRepository<Product> Products { get; }
        IGenericRepository<RawMaterial> RawMaterials { get; }
        IGenericRepository<ProductionBatch> ProductionBatches { get; }
        IGenericRepository<LabResult> LabResults { get; }
        IGenericRepository<ScrapAndQuarantine> ScrapAndQuarantines { get; }
        IGenericRepository<ShippingLog> ShippingLogs { get; }
        IGenericRepository<AuditLog> AuditLogs { get; }

        Task<int> SaveChangesAsync();
    }
}
