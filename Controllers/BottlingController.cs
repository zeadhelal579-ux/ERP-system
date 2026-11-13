// Exposes the role-scoped bottling operations.
using System.ComponentModel.DataAnnotations;
using ERP_system.Common;
using ERP_system.DTOs.Production;
using ERP_system.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP_system.Controllers;

[Route("api/v1/bottling")]
[Authorize]
public class BottlingController(BottlingService service) : BaseApiController
{
    [HttpGet("products")]
    [Authorize(Roles = "ProductionManager,SuperAdmin")]
    public async Task<IActionResult> GetProducts(CancellationToken ct) =>
        Success(await service.GetProductsAsync(ct));

    [HttpGet("issues")]
    [Authorize(Roles = "ProductionManager,SuperAdmin")]
    public async Task<IActionResult> GetIssues(CancellationToken ct) =>
        Success(await service.GetAvailableIssuesAsync(ct));

    [HttpPut("batches/{id:int}/hold")]
    [Authorize(Roles = "QualityManager,SuperAdmin")]
    public async Task<IActionResult> SetHold(int id, BatchHoldRequest request, CancellationToken ct) =>
        Success(await service.SetHoldAsync(id, request, CurrentUserId, ct));

    [HttpGet("packable-batches")]
    [Authorize(Roles = "Storekeeper,SuperAdmin")]
    public async Task<IActionResult> GetPackableBatches(CancellationToken ct) =>
        Success(await service.GetPackableBatchesAsync(ct));

    [HttpPost("batches/{id:int}/pallets")]
    [Authorize(Roles = "Storekeeper,SuperAdmin")]
    public async Task<IActionResult> Pack(int id, PackPalletRequest request, CancellationToken ct) =>
        Success(await service.PackAsync(id, request, CurrentUserId, ct));

    [HttpGet("pallets")]
    [Authorize(Roles = "Storekeeper,QualityManager,GeneralManager,SuperAdmin")]
    public async Task<IActionResult> GetPallets([FromQuery] int? batchId, CancellationToken ct) =>
        Success(await service.GetPalletsAsync(batchId, ct));

    [HttpPost("dispatch")]
    [Authorize(Roles = "Storekeeper,SuperAdmin")]
    public async Task<IActionResult> Dispatch(DispatchPalletsRequest request, CancellationToken ct) =>
        Success(await service.DispatchAsync(request, CurrentUserId, ct));

    [HttpGet("recall")]
    [Authorize(Roles = "QualityManager,GeneralManager,SuperAdmin")]
    public async Task<IActionResult> Recall([FromQuery, Required, StringLength(50)] string lotCode, CancellationToken ct) =>
        Success(await service.GetRecallAsync(lotCode, ct));
}
