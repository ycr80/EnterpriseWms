using EnterpriseWms.Contracts;

namespace EnterpriseWms.Application;

public interface IAuthService
{
    Task<CurrentUserDto?> ValidateCredentialsAsync(string username, string password, CancellationToken cancellationToken);
}

public interface ICatalogService
{
    Task<PagedResult<ProductDto>> GetProductsAsync(string? keyword, string? category, bool? active, int page, int pageSize, CancellationToken cancellationToken);
    Task<ProductDto> CreateProductAsync(SaveProductRequest request, CancellationToken cancellationToken);
    Task<ProductDto?> UpdateProductAsync(int id, SaveProductRequest request, CancellationToken cancellationToken);
    Task<bool> SetProductActiveAsync(int id, bool active, CancellationToken cancellationToken);
    Task<PagedResult<WarehouseDto>> GetWarehousesAsync(string? keyword, bool? active, int page, int pageSize, CancellationToken cancellationToken);
    Task<WarehouseDto> CreateWarehouseAsync(SaveWarehouseRequest request, CancellationToken cancellationToken);
    Task<WarehouseDto?> UpdateWarehouseAsync(int id, SaveWarehouseRequest request, CancellationToken cancellationToken);
    Task<bool> SetWarehouseActiveAsync(int id, bool active, CancellationToken cancellationToken);
}

public interface IInventoryService
{
    Task<PagedResult<InventoryDto>> GetInventoryAsync(int? warehouseId, string? keyword, string? category, bool warningOnly, int page, int pageSize, CancellationToken cancellationToken);
    Task<InventoryDto?> GetByCodesAsync(string warehouseCode, string sku, CancellationToken cancellationToken);
    Task<IReadOnlyList<InventoryDto>> GetWarningsByWarehouseCodeAsync(string? warehouseCode, CancellationToken cancellationToken);
    Task<bool> UpdateSafetyStockAsync(int inventoryId, decimal safetyStock, CancellationToken cancellationToken);
}

public interface IOrderService
{
    Task<StockOrderDto> PostInboundAsync(CreateStockOrderRequest request, int operatorId, CancellationToken cancellationToken);
    Task<StockOrderDto> PostOutboundAsync(CreateStockOrderRequest request, int operatorId, CancellationToken cancellationToken);
    Task<PagedResult<StockOrderDto>> GetOrdersAsync(string type, int? warehouseId, int page, int pageSize, CancellationToken cancellationToken);
    Task<StockOrderDto?> GetOrderAsync(string type, int id, CancellationToken cancellationToken);
}

public interface IReportService
{
    Task<DashboardDto> GetDashboardAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<InventoryDto>> GetInventoryDataAsync(int? warehouseId, bool warningOnly, CancellationToken cancellationToken);
    Task<byte[]> CreateInventoryExcelAsync(int? warehouseId, bool warningOnly, CancellationToken cancellationToken);
}

public interface IAdminService
{
    Task<IReadOnlyList<UserDto>> GetUsersAsync(CancellationToken cancellationToken);
    Task<UserDto> CreateUserAsync(SaveUserRequest request, CancellationToken cancellationToken);
    Task<bool> SetUserActiveAsync(int id, bool active, CancellationToken cancellationToken);
    Task<PagedResult<OperationLogDto>> GetLogsAsync(string? username, string? module, int page, int pageSize, CancellationToken cancellationToken);
}

public sealed class BusinessRuleException : Exception
{
    public BusinessRuleException(string code, string message) : base(message) => Code = code;
    public string Code { get; }
}

public static class OrderValidator
{
    public static void Validate(CreateStockOrderRequest request)
    {
        if (request.WarehouseId <= 0)
            throw new BusinessRuleException("validation.failed", "请选择仓库。");
        if (request.Items == null || request.Items.Count == 0)
            throw new BusinessRuleException("validation.failed", "单据至少包含一个商品。");
        if (request.Items.Any(x => x.ProductId <= 0 || x.Quantity <= 0))
            throw new BusinessRuleException("validation.failed", "商品和数量必须有效，数量必须大于零。");
        if (request.Items.GroupBy(x => x.ProductId).Any(x => x.Count() > 1))
            throw new BusinessRuleException("validation.failed", "同一商品不能在单据中重复出现。");
    }
}
