using ERP_system.DTOs.Manager;
using ERP_system.DTOs.Production;
using ERP_system.Exceptions;
using ERP_system.Models;
using ERP_system.Repositories;
using ERP_system.Services.Interfaces;

namespace ERP_system.Services
{
    public class ManagerService : IManagerService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;
        private readonly ILogger<ManagerService> _logger;

        public ManagerService(IUnitOfWork unitOfWork, IAuditService auditService, ILogger<ManagerService> logger)
        {
            _unitOfWork = unitOfWork;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task<BatchResponseDto> MakeDecisionAsync(ManagerDecisionDto request, int userId)
        {
            var batch = await _unitOfWork.ProductionBatches.GetByIdAsync(request.BatchId);

            if (batch == null)
                throw NotFoundException.For("التشغيلة", "BATCH_NOT_FOUND");

            // الموافقة مسموحة بس لو المعمل نجّح التشغيلة
            if (request.Approve)
            {
                if (batch.CurrentState != "QC_Passed" || batch.IsHeld)
                    throw new InvalidStateTransitionException(
                        $"لا يمكن الموافقة على التشغيلة وهي في حالة '{batch.CurrentState}'");

                batch.CurrentState = "Ready_For_Shipping";
                _unitOfWork.ProductionBatches.Update(batch);

                await _auditService.LogAsync(userId, "Manager_Approved", "Batch", batch.BatchNumber, request.Reason);
            }
            else
            {
                // الرفض مسموح من أي حالة QC_Passed أو QC_Failed
                if (batch.CurrentState != "QC_Passed" && batch.CurrentState != "QC_Failed")
                    throw new InvalidStateTransitionException(
                        $"لا يمكن رفض التشغيلة وهي في حالة '{batch.CurrentState}'");

                batch.CurrentState = "Quarantine";
                _unitOfWork.ProductionBatches.Update(batch);

                var scrapEntry = new ScrapAndQuarantine
                {
                    SourceType = "ManagerRejection",
                    ReferenceId = batch.Id,
                    Quantity = batch.ProducedQty,
                    ScrapReason = request.Reason ?? "رفض من المدير بدون سبب محدد",
                    FinalDecision = "Pending"
                };

                await _unitOfWork.ScrapAndQuarantines.AddAsync(scrapEntry);

                await _auditService.LogAsync(userId, "Manager_Rejected", "Batch", batch.BatchNumber, request.Reason);
            }

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("قرار مدير الجودة على التشغيلة {BatchNumber}: {Decision}",
                batch.BatchNumber, request.Approve ? "موافقة" : "رفض");

            return BatchMapper.ToDto(batch);
        }

        public async Task<IEnumerable<BatchSummaryDto>> GetPendingApprovalsAsync()
        {
            var batches = await _unitOfWork.ProductionBatches.FindAsync(b => b.CurrentState == "QC_Passed" || b.CurrentState == "QC_Failed");
            return batches.Select(b => new BatchSummaryDto
            {
                Id = b.Id,
                BatchNumber = b.BatchNumber,
                ProductId = b.ProductId,
                CurrentState = b.CurrentState,
                IsHeld = b.IsHeld,
                ProducedQty = b.ProducedQty,
                ExpiryDate = b.ExpiryDate,
                CreatedAt = b.CreatedAt
            });
        }
    }
}
