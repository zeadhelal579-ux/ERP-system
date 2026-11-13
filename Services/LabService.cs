using ERP_system.Common;
using ERP_system.DTOs.Lab;
using ERP_system.DTOs.Production;
using ERP_system.Exceptions;
using ERP_system.Models;
using ERP_system.Repositories;
using ERP_system.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ERP_system.Services
{
    public class LabService : ILabService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;
        private readonly ILogger<LabService> _logger;

        public LabService(IUnitOfWork unitOfWork, IAuditService auditService, ILogger<LabService> logger)
        {
            _unitOfWork = unitOfWork;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task<LabResultResponseDto> SubmitResultAsync(SubmitLabResultDto request, int userId)
        {
            // لازم مرجع واحد بس (خامة أو باتش)، مش الاتنين ومش ولا واحد
            var hasBatch = request.BatchId.HasValue;
            var hasMaterial = request.RawMaterialId.HasValue;

            if (hasBatch == hasMaterial) // لو الاتنين true أو الاتنين false
                throw new BusinessRuleException("INVALID_REFERENCE_COMBINATION",
                    "لازم تحديد مرجع واحد بس: إما خامة أو تشغيلة، مش الاتنين ومش ولا واحدة");

            if (request.InspectionType == "IncomingMaterial" && hasMaterial)
            {
                var material = await _unitOfWork.RawMaterials.GetByIdAsync(request.RawMaterialId!.Value);
                if (material == null)
                    throw NotFoundException.For("الخامة", "MATERIAL_NOT_FOUND");

                material.IsQCApproved = request.IsPassed;
                _unitOfWork.RawMaterials.Update(material);

                await _auditService.LogAsync(userId, request.IsPassed ? "Material_QC_Passed" : "Material_QC_Failed",
                    "RawMaterial", material.MaterialName, request.Notes);
            }
            else if (request.InspectionType == "FinishedBatch" && hasBatch)
            {
                var batch = await _unitOfWork.ProductionBatches.GetByIdAsync(request.BatchId!.Value);
                if (batch == null)
                    throw NotFoundException.For("التشغيلة", "BATCH_NOT_FOUND");

                if (batch.CurrentState != "Waiting_QC")
                    throw new InvalidStateTransitionException(
                        $"لا يمكن فحص التشغيلة وهي في حالة '{batch.CurrentState}'");

                batch.CurrentState = request.IsPassed ? "QC_Passed" : "QC_Failed";
                _unitOfWork.ProductionBatches.Update(batch);

                await _auditService.LogAsync(userId, request.IsPassed ? "Batch_QC_Passed" : "Batch_QC_Failed",
                    "Batch", batch.BatchNumber, request.Notes);
            }
            else
            {
                // تركيبة غير منطقية (مثلاً InspectionType متعارض مع المرجع المبعوت)
                throw new BusinessRuleException("INVALID_REFERENCE_COMBINATION",
                    "نوع الفحص لا يتطابق مع نوع المرجع المرسل");
            }

            var result = new LabResult
            {
                Type = request.InspectionType,
                BatchId = request.BatchId,
                RawMaterialId = request.RawMaterialId,
                ResultDetails = request.ResultDetails,
                IsPassed = request.IsPassed,
                Notes = request.Notes,
                TestedBy = userId,
                // Finding #12: توحيد كل توقيتات النظام على UTC
                TestedAt = DateTime.UtcNow
            };

            await _unitOfWork.LabResults.AddAsync(result);
            await _unitOfWork.SaveChangesAsync(); // تحديث الحالة + تسجيل النتيجة + الـ Audit سوا

            _logger.LogInformation("نتيجة فحص جديدة: {Type}, ناجحة: {Passed}", request.InspectionType, request.IsPassed);

            return MapToDto(result);
        }

        public async Task<PagedResult<LabResultResponseDto>> GetAllResultsAsync(PaginationParams pagination)
        {
            var query = _unitOfWork.LabResults.Query();

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderByDescending(r => r.Id)
                .Skip(pagination.Skip)
                .Take(pagination.PageSize)
                .ToListAsync();

            return new PagedResult<LabResultResponseDto>
            {
                Items = items.Select(MapToDto),
                Page = pagination.Page,
                PageSize = pagination.PageSize,
                TotalCount = totalCount
            };
        }

        public async Task<IEnumerable<PendingMaterialDto>> GetPendingIncomingMaterialsAsync()
        {
            var materials = await _unitOfWork.RawMaterials.FindAsync(m => !m.IsQCApproved);
            return materials.Select(m => new PendingMaterialDto
            {
                Id = m.Id,
                MaterialName = m.MaterialName,
                SupplierName = m.SupplierName,
                CurrentQuantity = m.CurrentQuantity,
                PhysicalState = m.PhysicalState,
                ExpiryDate = m.ExpiryDate
            });
        }

        public async Task<IEnumerable<BatchSummaryDto>> GetPendingBatchesAsync()
        {
            var batches = await _unitOfWork.ProductionBatches.FindAsync(b => b.CurrentState == "Waiting_QC");
            return batches.Select(b => new BatchSummaryDto
            {
                Id = b.Id,
                BatchNumber = b.BatchNumber,
                ProductId = b.ProductId,
                ProducedQty = b.ProducedQty,
                ExpiryDate = b.ExpiryDate,
                CreatedAt = b.CreatedAt
            });
        }

        private static LabResultResponseDto MapToDto(LabResult r)
        {
            return new LabResultResponseDto
            {
                Id = r.Id,
                InspectionType = r.Type,
                BatchId = r.BatchId,
                RawMaterialId = r.RawMaterialId,
                ResultDetails = r.ResultDetails,
                IsPassed = r.IsPassed,
                Notes = r.Notes,
                TestedAt = r.TestedAt
            };
        }
    }
}
