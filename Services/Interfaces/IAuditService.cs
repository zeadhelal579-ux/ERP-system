namespace ERP_system.Services.Interfaces
{
    public interface IAuditService
    {
        Task LogAsync(int userId, string actionType, string referenceType, string referenceValue, string? notes = null);
    }
}
