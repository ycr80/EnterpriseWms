using EnterpriseWms.Application;
using EnterpriseWms.Contracts;
using EnterpriseWms.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseWms.Api.Controllers;

[ApiController, Authorize(Roles = Roles.All), Route("api/products")]
public sealed class ProductsController : ControllerBase
{
    private readonly ICatalogService _service;
    public ProductsController(ICatalogService service) => _service = service;

    [HttpGet]
    public Task<PagedResult<ProductDto>> Get([FromQuery] string? keyword, [FromQuery] string? category, [FromQuery] bool? active, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default) =>
        _service.GetProductsAsync(keyword, category, active, page, pageSize, cancellationToken);

    [Authorize(Policy = "AdminOnly"), HttpPost]
    public async Task<ActionResult<ProductDto>> Create(SaveProductRequest request, CancellationToken cancellationToken) => Ok(await _service.CreateProductAsync(request, cancellationToken));

    [Authorize(Policy = "AdminOnly"), HttpPut("{id:int}")]
    public async Task<ActionResult<ProductDto>> Update(int id, SaveProductRequest request, CancellationToken cancellationToken)
    {
        var result = await _service.UpdateProductAsync(id, request, cancellationToken);
        return result == null ? NotFound() : Ok(result);
    }

    [Authorize(Policy = "AdminOnly"), HttpPatch("{id:int}/active")]
    public async Task<IActionResult> SetActive(int id, [FromQuery] bool value, CancellationToken cancellationToken) =>
        await _service.SetProductActiveAsync(id, value, cancellationToken) ? NoContent() : NotFound();
}

[ApiController, Authorize(Roles = Roles.All), Route("api/warehouses")]
public sealed class WarehousesController : ControllerBase
{
    private readonly ICatalogService _service;
    public WarehousesController(ICatalogService service) => _service = service;

    [HttpGet]
    public Task<PagedResult<WarehouseDto>> Get([FromQuery] string? keyword, [FromQuery] bool? active, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default) =>
        _service.GetWarehousesAsync(keyword, active, page, pageSize, cancellationToken);

    [Authorize(Policy = "AdminOnly"), HttpPost]
    public async Task<ActionResult<WarehouseDto>> Create(SaveWarehouseRequest request, CancellationToken cancellationToken) => Ok(await _service.CreateWarehouseAsync(request, cancellationToken));

    [Authorize(Policy = "AdminOnly"), HttpPut("{id:int}")]
    public async Task<ActionResult<WarehouseDto>> Update(int id, SaveWarehouseRequest request, CancellationToken cancellationToken)
    {
        var result = await _service.UpdateWarehouseAsync(id, request, cancellationToken);
        return result == null ? NotFound() : Ok(result);
    }

    [Authorize(Policy = "AdminOnly"), HttpPatch("{id:int}/active")]
    public async Task<IActionResult> SetActive(int id, [FromQuery] bool value, CancellationToken cancellationToken) =>
        await _service.SetWarehouseActiveAsync(id, value, cancellationToken) ? NoContent() : NotFound();
}
