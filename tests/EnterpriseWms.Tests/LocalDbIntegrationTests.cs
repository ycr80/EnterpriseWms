using EnterpriseWms.Application;
using EnterpriseWms.Contracts;
using EnterpriseWms.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Sdk;

namespace EnterpriseWms.Tests;

[AttributeUsage(AttributeTargets.Method)]
public sealed class LocalDbFactAttribute : FactAttribute
{
    public LocalDbFactAttribute()
    {
        if (!LocalDbTestDatabase.IsLocalDbInstalled()) Skip = "SQL Server LocalDB 未安装。";
    }
}

internal sealed class LocalDbTestDatabase : IAsyncDisposable
{
    private readonly string _databaseName = "EnterpriseWmsTests_" + Guid.NewGuid().ToString("N");
    public ServiceProvider Services { get; private set; } = null!;

    public static bool IsLocalDbInstalled()
    {
        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        return Directory.Exists(Path.Combine(programFiles, "Microsoft SQL Server")) &&
               Directory.EnumerateFiles(Path.Combine(programFiles, "Microsoft SQL Server"), "SqlLocalDB.exe", SearchOption.AllDirectories).Any();
    }

    public async Task InitializeAsync()
    {
        var connection = $"Server=(localdb)\\MSSQLLocalDB;Database={_databaseName};Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:WarehouseDb"] = connection }).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInfrastructure(configuration);
        Services = services.BuildServiceProvider();
        await DatabaseInitializer.InitializeAsync(Services);
    }

    public async ValueTask DisposeAsync()
    {
        if (Services == null) return;
        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<WarehouseDbContext>();
        await db.Database.EnsureDeletedAsync();
        await Services.DisposeAsync();
    }
}

public sealed class LocalDbIntegrationTests
{
    [LocalDbFact]
    public async Task MultiLineInboundUpdatesInventoryAndLedgerAtomically()
    {
        await using var database = new LocalDbTestDatabase();
        await database.InitializeAsync();
        await using var scope = database.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<WarehouseDbContext>();
        var service = scope.ServiceProvider.GetRequiredService<IOrderService>();
        var warehouse = await db.Warehouses.FirstAsync();
        var products = await db.Products.Take(2).ToListAsync();
        var user = await db.Users.FirstAsync(x => x.Username == "admin");
        var before = await db.Inventories.Where(x => x.WarehouseId == warehouse.Id && products.Select(p => p.Id).Contains(x.ProductId)).ToDictionaryAsync(x => x.ProductId, x => x.Quantity);

        var order = await service.PostInboundAsync(new CreateStockOrderRequest
        {
            WarehouseId = warehouse.Id,
            Items = products.Select(x => new StockOrderItemRequest { ProductId = x.Id, Quantity = 5, UnitCost = 12.5m }).ToList()
        }, user.Id, default);

        db.ChangeTracker.Clear();
        var after = await db.Inventories.Where(x => x.WarehouseId == warehouse.Id && products.Select(p => p.Id).Contains(x.ProductId)).ToListAsync();
        Assert.All(after, x => Assert.Equal(before[x.ProductId] + 5, x.Quantity));
        Assert.Equal(2, await db.StockMovements.CountAsync(x => x.SourceOrderNo == order.OrderNo));
    }

    [LocalDbFact]
    public async Task InsufficientItemRollsBackEntireOutboundOrder()
    {
        await using var database = new LocalDbTestDatabase();
        await database.InitializeAsync();
        await using var scope = database.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<WarehouseDbContext>();
        var service = scope.ServiceProvider.GetRequiredService<IOrderService>();
        var warehouse = await db.Warehouses.FirstAsync();
        var inventories = await db.Inventories.Where(x => x.WarehouseId == warehouse.Id).Take(2).ToListAsync();
        var user = await db.Users.FirstAsync(x => x.Username == "admin");
        var beforeOrders = await db.OutboundOrders.CountAsync();
        var before = inventories.ToDictionary(x => x.ProductId, x => x.Quantity);

        var exception = await Assert.ThrowsAsync<BusinessRuleException>(() => service.PostOutboundAsync(new CreateStockOrderRequest
        {
            WarehouseId = warehouse.Id,
            Items = new List<StockOrderItemRequest>
            {
                new() { ProductId = inventories[0].ProductId, Quantity = 1 },
                new() { ProductId = inventories[1].ProductId, Quantity = inventories[1].Quantity + 1 }
            }
        }, user.Id, default));

        Assert.Equal("inventory.insufficient", exception.Code);
        db.ChangeTracker.Clear();
        Assert.Equal(beforeOrders, await db.OutboundOrders.CountAsync());
        var after = await db.Inventories.Where(x => x.WarehouseId == warehouse.Id && before.Keys.Contains(x.ProductId)).ToListAsync();
        Assert.All(after, x => Assert.Equal(before[x.ProductId], x.Quantity));
    }

    [LocalDbFact]
    public async Task ConcurrentOutboundAllowsOnlyOneOrder()
    {
        await using var database = new LocalDbTestDatabase();
        await database.InitializeAsync();
        int warehouseId, productId, userId;
        await using (var setup = database.Services.CreateAsyncScope())
        {
            var db = setup.ServiceProvider.GetRequiredService<WarehouseDbContext>();
            var inventory = await db.Inventories.FirstAsync();
            inventory.Quantity = 10;
            await db.SaveChangesAsync();
            warehouseId = inventory.WarehouseId; productId = inventory.ProductId; userId = (await db.Users.FirstAsync(x => x.Username == "admin")).Id;
        }

        async Task<Exception?> ExecuteAsync()
        {
            await using var scope = database.Services.CreateAsyncScope();
            try
            {
                await scope.ServiceProvider.GetRequiredService<IOrderService>().PostOutboundAsync(new CreateStockOrderRequest
                {
                    WarehouseId = warehouseId, Items = new List<StockOrderItemRequest> { new() { ProductId = productId, Quantity = 7 } }
                }, userId, default);
                return null;
            }
            catch (Exception exception) { return exception; }
        }

        var results = await Task.WhenAll(ExecuteAsync(), ExecuteAsync());
        Assert.Single(results, x => x == null);
        var failure = Assert.IsType<BusinessRuleException>(Assert.Single(results, x => x != null));
        Assert.Equal("inventory.insufficient", failure.Code);
        await using var verify = database.Services.CreateAsyncScope();
        var verifyDb = verify.ServiceProvider.GetRequiredService<WarehouseDbContext>();
        Assert.Equal(3, await verifyDb.Inventories.Where(x => x.WarehouseId == warehouseId && x.ProductId == productId).Select(x => x.Quantity).SingleAsync());
        Assert.Equal(1, await verifyDb.OutboundOrders.CountAsync());
    }

    [LocalDbFact]
    public async Task CatalogAndInventoryCombinationFiltersReturnAccuratePages()
    {
        await using var database = new LocalDbTestDatabase();
        await database.InitializeAsync();
        await using var scope = database.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<WarehouseDbContext>();
        var catalog = scope.ServiceProvider.GetRequiredService<ICatalogService>();
        var inventory = scope.ServiceProvider.GetRequiredService<IInventoryService>();
        var warehouse = await db.Warehouses.OrderBy(x => x.Code).FirstAsync();
        var product = await db.Products.OrderBy(x => x.Sku).FirstAsync();

        var productPage = await catalog.GetProductsAsync(product.Sku, product.Category, true, 1, 1, default);
        Assert.Equal(1, productPage.TotalCount);
        Assert.Equal(product.Sku, Assert.Single(productPage.Items).Sku);

        var warehousePage = await catalog.GetWarehousesAsync(warehouse.Code, true, 1, 1, default);
        Assert.Equal(1, warehousePage.TotalCount);
        Assert.Equal(warehouse.Code, Assert.Single(warehousePage.Items).Code);

        var inventoryPage = await inventory.GetInventoryAsync(warehouse.Id, product.Sku, product.Category, false, 1, 1, default);
        Assert.Equal(1, inventoryPage.TotalCount);
        Assert.Equal(product.Sku, Assert.Single(inventoryPage.Items).Sku);

        var pagedWarehouseInventory = await inventory.GetInventoryAsync(warehouse.Id, null, null, false, 1, 2, default);
        Assert.Equal(5, pagedWarehouseInventory.TotalCount);
        Assert.Equal(2, pagedWarehouseInventory.Items.Count);
        Assert.Equal(3, pagedWarehouseInventory.TotalPages);

        var warnings = await inventory.GetInventoryAsync(null, null, null, true, 1, 20, default);
        Assert.NotEmpty(warnings.Items);
        Assert.All(warnings.Items, x => Assert.True(x.IsLowStock));
    }

    [LocalDbFact]
    public async Task InactiveProductCannotEnterNewOrderWhileHistoricalOrderRemainsQueryable()
    {
        await using var database = new LocalDbTestDatabase();
        await database.InitializeAsync();
        await using var scope = database.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<WarehouseDbContext>();
        var catalog = scope.ServiceProvider.GetRequiredService<ICatalogService>();
        var orders = scope.ServiceProvider.GetRequiredService<IOrderService>();
        var warehouse = await db.Warehouses.FirstAsync();
        var product = await db.Products.FirstAsync();
        var user = await db.Users.FirstAsync(x => x.Username == "admin");
        var request = new CreateStockOrderRequest
        {
            WarehouseId = warehouse.Id,
            Items = new List<StockOrderItemRequest> { new() { ProductId = product.Id, Quantity = 1, UnitCost = 10 } }
        };

        var historical = await orders.PostInboundAsync(request, user.Id, default);
        Assert.True(await catalog.SetProductActiveAsync(product.Id, false, default));

        var exception = await Assert.ThrowsAsync<BusinessRuleException>(() => orders.PostOutboundAsync(request, user.Id, default));
        Assert.Equal("product.inactive", exception.Code);

        var loadedHistorical = await orders.GetOrderAsync("inbound", historical.Id, default);
        Assert.NotNull(loadedHistorical);
        Assert.Equal(historical.OrderNo, loadedHistorical.OrderNo);
        Assert.Equal(product.Id, Assert.Single(loadedHistorical.Items).ProductId);
    }
}
