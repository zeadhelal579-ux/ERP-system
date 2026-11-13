using ERP_system.Data;
using ERP_system.Models;

namespace ERP_system.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;

        private IGenericRepository<Role>? _roles;
        private IGenericRepository<User>? _users;
        private IGenericRepository<Product>? _products;
        private IGenericRepository<RawMaterial>? _rawMaterials;
        private IGenericRepository<ProductionBatch>? _productionBatches;
        private IGenericRepository<LabResult>? _labResults;
        private IGenericRepository<ScrapAndQuarantine>? _scrapAndQuarantines;
        private IGenericRepository<ShippingLog>? _shippingLogs;
        private IGenericRepository<AuditLog>? _auditLogs;

        public UnitOfWork(AppDbContext context)
        {
            _context = context;
        }

        public IGenericRepository<Role> Roles =>
            _roles ??= new GenericRepository<Role>(_context);

        public IGenericRepository<User> Users =>
            _users ??= new GenericRepository<User>(_context);

        public IGenericRepository<Product> Products =>
            _products ??= new GenericRepository<Product>(_context);

        public IGenericRepository<RawMaterial> RawMaterials =>
            _rawMaterials ??= new GenericRepository<RawMaterial>(_context);

        public IGenericRepository<ProductionBatch> ProductionBatches =>
            _productionBatches ??= new GenericRepository<ProductionBatch>(_context);

        public IGenericRepository<LabResult> LabResults =>
            _labResults ??= new GenericRepository<LabResult>(_context);

        public IGenericRepository<ScrapAndQuarantine> ScrapAndQuarantines =>
            _scrapAndQuarantines ??= new GenericRepository<ScrapAndQuarantine>(_context);

        public IGenericRepository<ShippingLog> ShippingLogs =>
            _shippingLogs ??= new GenericRepository<ShippingLog>(_context);

        public IGenericRepository<AuditLog> AuditLogs =>
            _auditLogs ??= new GenericRepository<AuditLog>(_context);

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
