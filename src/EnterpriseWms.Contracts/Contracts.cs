namespace EnterpriseWms.Contracts;

public sealed class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public sealed class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public sealed class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public CurrentUserDto User { get; set; } = new CurrentUserDto();
}

public sealed class CurrentUserDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

public sealed class ProductDto
{
    public int Id { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Specification { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public sealed class SaveProductRequest
{
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Specification { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
}

public sealed class WarehouseDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public sealed class SaveWarehouseRequest
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
}

public sealed class InventoryDto
{
    public int Id { get; set; }
    public int WarehouseId { get; set; }
    public string WarehouseCode { get; set; } = string.Empty;
    public string WarehouseName { get; set; } = string.Empty;
    public int ProductId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Specification { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal SafetyStock { get; set; }
    public bool IsLowStock { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

public sealed class UpdateSafetyStockRequest
{
    public decimal SafetyStock { get; set; }
}

public sealed class StockOrderItemRequest
{
    public int ProductId { get; set; }
    public decimal Quantity { get; set; }
    public decimal? UnitCost { get; set; }
}

public sealed class CreateStockOrderRequest
{
    public int WarehouseId { get; set; }
    public string Remark { get; set; } = string.Empty;
    public List<StockOrderItemRequest> Items { get; set; } = new List<StockOrderItemRequest>();
}

public sealed class StockOrderDto
{
    public int Id { get; set; }
    public string OrderNo { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public int WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public string OperatorName { get; set; } = string.Empty;
    public string Remark { get; set; } = string.Empty;
    public DateTime PostedAtUtc { get; set; }
    public List<StockOrderLineDto> Items { get; set; } = new List<StockOrderLineDto>();
}

public sealed class StockOrderLineDto
{
    public int ProductId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal? UnitCost { get; set; }
}

public sealed class DashboardDto
{
    public int ActiveProductCount { get; set; }
    public int ActiveWarehouseCount { get; set; }
    public int LowStockCount { get; set; }
    public int TodayInboundCount { get; set; }
    public int TodayOutboundCount { get; set; }
    public List<ChartPointDto> OrderTrend { get; set; } = new List<ChartPointDto>();
    public List<ChartPointDto> WarningByWarehouse { get; set; } = new List<ChartPointDto>();
}

public sealed class ChartPointDto
{
    public string Label { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public decimal SecondaryValue { get; set; }
}

public sealed class UserDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime? LastLoginAtUtc { get; set; }
}

public sealed class SaveUserRequest
{
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public sealed class OperationLogDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Target { get; set; } = string.Empty;
    public string Result { get; set; } = string.Empty;
    public int ElapsedMilliseconds { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}
