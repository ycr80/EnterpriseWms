using EnterpriseWms.Application;
using EnterpriseWms.Contracts;
using EnterpriseWms.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseWms.Api.Controllers;

[ApiController, Authorize(Roles = Roles.All), Route("api/reports")]
public sealed class ReportsController : ControllerBase
{
    private readonly IReportService _service;
    public ReportsController(IReportService service) => _service = service;
    [HttpGet("dashboard")]
    public Task<DashboardDto> Dashboard(CancellationToken cancellationToken) => _service.GetDashboardAsync(cancellationToken);
    [HttpGet("inventory-data")]
    public Task<IReadOnlyList<InventoryDto>> InventoryData([FromQuery] int? warehouseId, [FromQuery] bool warningOnly = false, CancellationToken cancellationToken = default) => _service.GetInventoryDataAsync(warehouseId, warningOnly, cancellationToken);
    [HttpGet("inventory.xlsx")]
    public async Task<IActionResult> InventoryExcel([FromQuery] int? warehouseId, [FromQuery] bool warningOnly = false, CancellationToken cancellationToken = default)
    {
        var bytes = await _service.CreateInventoryExcelAsync(warehouseId, warningOnly, cancellationToken);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"库存报表_{DateTime.Now:yyyyMMddHHmm}.xlsx");
    }
}

[ApiController, Authorize(Policy = "AdminOnly"), Route("api/admin")]
public sealed class AdminController : ControllerBase
{
    private readonly IAdminService _service;
    public AdminController(IAdminService service) => _service = service;
    [HttpGet("users")]
    public Task<IReadOnlyList<UserDto>> Users(CancellationToken cancellationToken) => _service.GetUsersAsync(cancellationToken);
    [HttpPost("users")]
    public async Task<ActionResult<UserDto>> CreateUser(SaveUserRequest request, CancellationToken cancellationToken) => Ok(await _service.CreateUserAsync(request, cancellationToken));
    [HttpPatch("users/{id:int}/active")]
    public async Task<IActionResult> SetUserActive(int id, [FromQuery] bool value, CancellationToken cancellationToken) => await _service.SetUserActiveAsync(id, value, cancellationToken) ? NoContent() : NotFound();
    [HttpGet("operation-logs")]
    public Task<PagedResult<OperationLogDto>> Logs([FromQuery] string? username, [FromQuery] string? module, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken cancellationToken = default) => _service.GetLogsAsync(username, module, page, pageSize, cancellationToken);
}
