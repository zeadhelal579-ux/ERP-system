using ERP_system.Models;
using ERP_system.Repositories;
using ERP_system.Services.Interfaces;

namespace ERP_system.Services
{
    public class AuditService : IAuditService
    {
        private readonly IUnitOfWork _unitOfWork;

        public AuditService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task LogAsync(int userId, string actionType, string referenceType, string referenceValue, string? notes = null)
        {
            var log = new AuditLog
            {
                UserId = userId,
                Action = actionType,
                // Finding #12: توحيد كل توقيتات النظام على UTC
                Timestamp = DateTime.UtcNow,
                Details = $"{referenceType}: {referenceValue}" + (notes != null ? $" | {notes}" : "")
            };

            await _unitOfWork.AuditLogs.AddAsync(log);
            // ملاحظة: مفيش SaveChangesAsync هنا، هيتحفظ مع نفس الـ Transaction بتاعة العملية الأصلية
        }
    }
}
