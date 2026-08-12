using EnterpriseWms.Application;
using EnterpriseWms.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseWms.Api.Controllers;

[ApiController, Authorize, Route("api/inbound-orders")]
public sealed class InboundOrdersController : ControllerBase
{
    private readonly IOrderService _service;
    public InboundOrdersController(IOrderService service) => _service = service;
    [HttpGet]
    public Task<PagedResult<StockOrderDto>> Get([FromQuery] int? warehouseId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default) => _service.GetOrdersAsync("inbound", warehouseId, page, pageSize, cancellationToken);
    [HttpGet("{id:int}")]
    public async Task<ActionResult<StockOrderDto>> GetById(int id, CancellationToken cancellationToken) => (await _service.GetOrderAsync("inbound", id, cancellationToken)) is { } result ? Ok(result) : NotFound();
    [Authorize(Policy = "CanOperate"), HttpPost]
    public async Task<ActionResult<StockOrderDto>> Create(CreateStockOrderRequest request, CancellationToken cancellationToken) => Ok(await _service.PostInboundAsync(request, User.RequiredUserId(), cancellationToken));
}

[ApiController, Authorize, Route("api/outbound-orders")]
public sealed class OutboundOrdersController : ControllerBase
{
    private readonly IOrderService _service;
    public OutboundOrdersController(IOrderService service) => _service = service;
    [HttpGet]
    public Task<PagedResult<StockOrderDto>> Get([FromQuery] int? warehouseId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default) => _service.GetOrdersAsync("outbound", warehouseId, page, pageSize, cancellationToken);
    [HttpGet("{id:int}")]
    public async Task<ActionResult<StockOrderDto>> GetById(int id, CancellationToken cancellationToken) => (await _service.GetOrderAsync("outbound", id, cancellationToken)) is { } result ? Ok(result) : NotFound();
    [Authorize(Policy = "CanOperate"), HttpPost]
    public async Task<ActionResult<StockOrderDto>> Create(CreateStockOrderRequest request, CancellationToken cancellationToken) => Ok(await _service.PostOutboundAsync(request, User.RequiredUserId(), cancellationToken));
}
