using EnterpriseWms.Application;
using EnterpriseWms.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseWms.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("WarehouseDb")
            ?? throw new InvalidOperationException("缺少 WarehouseDb 连接字符串。");
        services.AddDbContext<WarehouseDbContext>(options => options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure()));
        services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICatalogService, CatalogService>();
        services.AddScoped<IInventoryService, InventoryService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<IAdminService, AdminService>();
        return services;
    }
}

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<WarehouseDbContext>();
        await db.Database.MigrateAsync(cancellationToken);
        await SeedAsync(db, scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>(), cancellationToken);
    }

    private static async Task SeedAsync(WarehouseDbContext db, IPasswordHasher<User> hasher, CancellationToken cancellationToken)
    {
        if (!await db.Users.AnyAsync(cancellationToken))
        {
            foreach (var (username, displayName, role, password) in new[]
            {
                ("admin", "系统管理员", Roles.Admin, "Admin123!"),
                ("operator", "仓库操作员", Roles.Operator, "Operator123!"),
                ("viewer", "只读查看员", Roles.Viewer, "Viewer123!")
            })
            {
                var user = new User { Username = username, DisplayName = displayName, Role = role };
                user.PasswordHash = hasher.HashPassword(user, password);
                db.Users.Add(user);
            }
            await db.SaveChangesAsync(cancellationToken);
        }

        if (!await db.Warehouses.AnyAsync(cancellationToken))
        {
            db.Warehouses.AddRange(
                new Warehouse { Code = "WH-SH-01", Name = "上海主仓", Address = "上海市浦东新区" },
                new Warehouse { Code = "WH-SH-02", Name = "上海备件仓", Address = "上海市闵行区" });
            await db.SaveChangesAsync(cancellationToken);
        }

        if (!await db.Products.AnyAsync(cancellationToken))
        {
            db.Products.AddRange(
                new Product { Sku = "ELEC-001", Name = "工业扫码枪", Category = "电子设备", Specification = "USB/二维码", Unit = "台" },
                new Product { Sku = "ELEC-002", Name = "标签打印机", Category = "电子设备", Specification = "热敏 300dpi", Unit = "台" },
                new Product { Sku = "PACK-001", Name = "瓦楞纸箱", Category = "包装耗材", Specification = "600×400×400mm", Unit = "个" },
                new Product { Sku = "PACK-002", Name = "封箱胶带", Category = "包装耗材", Specification = "60mm×100m", Unit = "卷" },
                new Product { Sku = "SAFE-001", Name = "防割手套", Category = "劳保用品", Specification = "L码", Unit = "双" });
            await db.SaveChangesAsync(cancellationToken);
        }

        if (!await db.Inventories.AnyAsync(cancellationToken))
        {
            var warehouses = await db.Warehouses.OrderBy(x => x.Id).ToListAsync(cancellationToken);
            var products = await db.Products.OrderBy(x => x.Id).ToListAsync(cancellationToken);
            foreach (var warehouse in warehouses)
            foreach (var product in products)
            {
                var baseQuantity = warehouse.Code.EndsWith("01", StringComparison.Ordinal) ? 50m : 12m;
                var quantity = product.Sku == "ELEC-002" && warehouse.Code.EndsWith("02", StringComparison.Ordinal) ? 3m : baseQuantity;
                db.Inventories.Add(new Inventory { WarehouseId = warehouse.Id, ProductId = product.Id, Quantity = quantity, SafetyStock = product.Category == "电子设备" ? 5m : 10m });
            }
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
