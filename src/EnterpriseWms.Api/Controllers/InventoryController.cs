using EnterpriseWms.Application;
using EnterpriseWms.Contracts;
using EnterpriseWms.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseWms.Api.Controllers;

[ApiController, Authorize(Roles = Roles.All), Route("api/inventory")]
public sealed class InventoryController : ControllerBase
{
    private readonly IInventoryService _service;
    public InventoryController(IInventoryService service) => _service = service;

    [HttpGet]
    public Task<PagedResult<InventoryDto>> Get([FromQuery] int? warehouseId, [FromQuery] string? keyword, [FromQuery] string? category, [FromQuery] bool warningOnly = false, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken cancellationToken = default) =>
        _service.GetInventoryAsync(warehouseId, keyword, category, warningOnly, page, pageSize, cancellationToken);

    [Authorize(Policy = "AdminOnly"), HttpPatch("{id:int}/safety-stock")]
    public async Task<IActionResult> UpdateSafetyStock(int id, UpdateSafetyStockRequest request, CancellationToken cancellationToken) =>
        await _service.UpdateSafetyStockAsync(id, request.SafetyStock, cancellationToken) ? NoContent() : NotFound();
}
