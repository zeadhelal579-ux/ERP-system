using ERP_system.Common;
using ERP_system.DTOs.Store;

namespace ERP_system.Services.Interfaces
{
    public interface IStoreService
    {
        Task<RawMaterialResponseDto> AddRawMaterialAsync(AddRawMaterialDto request, int userId);
        Task<PagedResult<RawMaterialResponseDto>> GetAllMaterialsAsync(PaginationParams pagination);
        Task<IEnumerable<RawMaterialResponseDto>> GetApprovedMaterialsAsync();

        /// <summary>يطلق NotFoundException أو BusinessRuleException حسب سبب الفشل. بيرجّع الخامة بعد تحديث الكمية.</summary>
        Task<RawMaterialResponseDto> IssueMaterialAsync(IssueMaterialDto request, int userId);

        /// <summary>يطلق NotFoundException أو BusinessRuleException حسب سبب الفشل. بيرجّع الخامة بعد تحديث الكمية.</summary>
        Task<RawMaterialResponseDto> RecordMaterialWasteAsync(RecordMaterialWasteDto request, int userId);
    }
}
