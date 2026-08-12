using EnterpriseWms.Application;
using EnterpriseWms.Contracts;
using EnterpriseWms.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseWms.Infrastructure;

public sealed class AuthService : IAuthService
{
    private readonly WarehouseDbContext _db;
    private readonly IPasswordHasher<User> _hasher;

    public AuthService(WarehouseDbContext db, IPasswordHasher<User> hasher)
    {
        _db = db;
        _hasher = hasher;
    }

    public async Task<CurrentUserDto?> ValidateCredentialsAsync(string username, string password, CancellationToken cancellationToken)
    {
        var normalized = (username ?? string.Empty).Trim();
        var user = await _db.Users.SingleOrDefaultAsync(x => x.Username == normalized && x.IsActive, cancellationToken);
        if (user == null || _hasher.VerifyHashedPassword(user, user.PasswordHash, password ?? string.Empty) == PasswordVerificationResult.Failed)
            return null;

        user.LastLoginAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return new CurrentUserDto { Id = user.Id, Username = user.Username, DisplayName = user.DisplayName, Role = user.Role };
    }
}

public sealed class CatalogService : ICatalogService
{
    private readonly WarehouseDbContext _db;
    public CatalogService(WarehouseDbContext db) => _db = db;

    public async Task<PagedResult<ProductDto>> GetProductsAsync(string? keyword, string? category, bool? active, int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = _db.Products.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var value = keyword.Trim();
            query = query.Where(x => x.Sku.Contains(value) || x.Name.Contains(value) || x.Specification.Contains(value));
        }
        if (!string.IsNullOrWhiteSpace(category)) query = query.Where(x => x.Category == category.Trim());
        if (active.HasValue) query = query.Where(x => x.IsActive == active.Value);
        var total = await query.CountAsync(cancellationToken);
        var (safePage, safeSize) = Page(page, pageSize);
        var items = await query.OrderBy(x => x.Sku).Skip((safePage - 1) * safeSize).Take(safeSize)
            .Select(x => new ProductDto { Id = x.Id, Sku = x.Sku, Name = x.Name, Category = x.Category, Specification = x.Specification, Unit = x.Unit, IsActive = x.IsActive })
            .ToListAsync(cancellationToken);
        return new PagedResult<ProductDto> { Items = items, Page = safePage, PageSize = safeSize, TotalCount = total };
    }

    public async Task<ProductDto> CreateProductAsync(SaveProductRequest request, CancellationToken cancellationToken)
    {
        ValidateProduct(request);
        var sku = request.Sku.Trim().ToUpperInvariant();
        if (await _db.Products.AnyAsync(x => x.Sku == sku, cancellationToken))
            throw new BusinessRuleException("product.sku_exists", "商品 SKU 已存在。");
        var entity = new Product { Sku = sku, Name = request.Name.Trim(), Category = request.Category.Trim(), Specification = request.Specification.Trim(), Unit = request.Unit.Trim() };
        _db.Products.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<ProductDto?> UpdateProductAsync(int id, SaveProductRequest request, CancellationToken cancellationToken)
    {
        ValidateProduct(request);
        var entity = await _db.Products.FindAsync(new object[] { id }, cancellationToken);
        if (entity == null) return null;
        var sku = request.Sku.Trim().ToUpperInvariant();
        if (await _db.Products.AnyAsync(x => x.Id != id && x.Sku == sku, cancellationToken))
            throw new BusinessRuleException("product.sku_exists", "商品 SKU 已存在。");
        entity.Sku = sku;
        entity.Name = request.Name.Trim();
        entity.Category = request.Category.Trim();
        entity.Specification = request.Specification.Trim();
        entity.Unit = request.Unit.Trim();
        await _db.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<bool> SetProductActiveAsync(int id, bool active, CancellationToken cancellationToken)
    {
        var entity = await _db.Products.FindAsync(new object[] { id }, cancellationToken);
        if (entity == null) return false;
        entity.IsActive = active;
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<PagedResult<WarehouseDto>> GetWarehousesAsync(string? keyword, bool? active, int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = _db.Warehouses.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var value = keyword.Trim();
            query = query.Where(x => x.Code.Contains(value) || x.Name.Contains(value) || x.Address.Contains(value));
        }
        if (active.HasValue) query = query.Where(x => x.IsActive == active.Value);
        var total = await query.CountAsync(cancellationToken);
        var (safePage, safeSize) = Page(page, pageSize);
        var items = await query.OrderBy(x => x.Code).Skip((safePage - 1) * safeSize).Take(safeSize)
            .Select(x => new WarehouseDto { Id = x.Id, Code = x.Code, Name = x.Name, Address = x.Address, IsActive = x.IsActive })
            .ToListAsync(cancellationToken);
        return new PagedResult<WarehouseDto> { Items = items, Page = safePage, PageSize = safeSize, TotalCount = total };
    }

    public async Task<WarehouseDto> CreateWarehouseAsync(SaveWarehouseRequest request, CancellationToken cancellationToken)
    {
        ValidateWarehouse(request);
        var code = request.Code.Trim().ToUpperInvariant();
        if (await _db.Warehouses.AnyAsync(x => x.Code == code, cancellationToken))
            throw new BusinessRuleException("warehouse.code_exists", "仓库编码已存在。");
        var entity = new Warehouse { Code = code, Name = request.Name.Trim(), Address = request.Address.Trim() };
        _db.Warehouses.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<WarehouseDto?> UpdateWarehouseAsync(int id, SaveWarehouseRequest request, CancellationToken cancellationToken)
    {
        ValidateWarehouse(request);
        var entity = await _db.Warehouses.FindAsync(new object[] { id }, cancellationToken);
        if (entity == null) return null;
        var code = request.Code.Trim().ToUpperInvariant();
        if (await _db.Warehouses.AnyAsync(x => x.Id != id && x.Code == code, cancellationToken))
            throw new BusinessRuleException("warehouse.code_exists", "仓库编码已存在。");
        entity.Code = code;
        entity.Name = request.Name.Trim();
        entity.Address = request.Address.Trim();
        await _db.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<bool> SetWarehouseActiveAsync(int id, bool active, CancellationToken cancellationToken)
    {
        var entity = await _db.Warehouses.FindAsync(new object[] { id }, cancellationToken);
        if (entity == null) return false;
        entity.IsActive = active;
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static (int Page, int Size) Page(int page, int pageSize) => (Math.Max(1, page), Math.Clamp(pageSize, 1, 100));
    private static void ValidateProduct(SaveProductRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Sku) || string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Category) || string.IsNullOrWhiteSpace(request.Unit))
            throw new BusinessRuleException("validation.failed", "SKU、商品名称、分类和单位不能为空。");
    }
    private static void ValidateWarehouse(SaveWarehouseRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.Name))
            throw new BusinessRuleException("validation.failed", "仓库编码和名称不能为空。");
    }
    private static ProductDto Map(Product x) => new() { Id = x.Id, Sku = x.Sku, Name = x.Name, Category = x.Category, Specification = x.Specification, Unit = x.Unit, IsActive = x.IsActive };
    private static WarehouseDto Map(Warehouse x) => new() { Id = x.Id, Code = x.Code, Name = x.Name, Address = x.Address, IsActive = x.IsActive };
}

public sealed class AdminService : IAdminService
{
    private static readonly string[] ValidRoles = { Roles.Admin, Roles.Operator, Roles.Viewer };
    private readonly WarehouseDbContext _db;
    private readonly IPasswordHasher<User> _hasher;
    public AdminService(WarehouseDbContext db, IPasswordHasher<User> hasher) { _db = db; _hasher = hasher; }

    public async Task<IReadOnlyList<UserDto>> GetUsersAsync(CancellationToken cancellationToken) =>
        await _db.Users.AsNoTracking().OrderBy(x => x.Username)
            .Select(x => new UserDto { Id = x.Id, Username = x.Username, DisplayName = x.DisplayName, Role = x.Role, IsActive = x.IsActive, LastLoginAtUtc = x.LastLoginAtUtc })
            .ToListAsync(cancellationToken);

    public async Task<UserDto> CreateUserAsync(SaveUserRequest request, CancellationToken cancellationToken)
    {
        var username = (request.Username ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(request.DisplayName) || string.IsNullOrWhiteSpace(request.Password))
            throw new BusinessRuleException("validation.failed", "用户名、显示名称和密码不能为空。");
        if (request.Password.Length < 8) throw new BusinessRuleException("validation.failed", "密码至少需要 8 个字符。");
        if (!ValidRoles.Contains(request.Role)) throw new BusinessRuleException("validation.failed", "角色无效。");
        if (await _db.Users.AnyAsync(x => x.Username == username, cancellationToken))
            throw new BusinessRuleException("user.username_exists", "用户名已存在。");
        var user = new User { Username = username, DisplayName = request.DisplayName.Trim(), Role = request.Role };
        user.PasswordHash = _hasher.HashPassword(user, request.Password);
        _db.Users.Add(user);
        await _db.SaveChangesAsync(cancellationToken);
        return new UserDto { Id = user.Id, Username = user.Username, DisplayName = user.DisplayName, Role = user.Role, IsActive = true };
    }

    public async Task<bool> SetUserActiveAsync(int id, bool active, CancellationToken cancellationToken)
    {
        var user = await _db.Users.FindAsync(new object[] { id }, cancellationToken);
        if (user == null) return false;
        user.IsActive = active;
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<PagedResult<OperationLogDto>> GetLogsAsync(string? username, string? module, int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = _db.OperationLogs.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(username)) query = query.Where(x => x.Username.Contains(username.Trim()));
        if (!string.IsNullOrWhiteSpace(module)) query = query.Where(x => x.Module == module.Trim());
        var safePage = Math.Max(1, page);
        var safeSize = Math.Clamp(pageSize, 1, 100);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(x => x.CreatedAtUtc).Skip((safePage - 1) * safeSize).Take(safeSize)
            .Select(x => new OperationLogDto { Id = x.Id, Username = x.Username, Module = x.Module, Action = x.Action, Target = x.Target, Result = x.Result, ElapsedMilliseconds = x.ElapsedMilliseconds, IpAddress = x.IpAddress, CreatedAtUtc = x.CreatedAtUtc })
            .ToListAsync(cancellationToken);
        return new PagedResult<OperationLogDto> { Items = items, Page = safePage, PageSize = safeSize, TotalCount = total };
    }
}
