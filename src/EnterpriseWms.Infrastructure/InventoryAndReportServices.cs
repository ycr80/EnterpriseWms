using ClosedXML.Excel;
using System.Data;
using EnterpriseWms.Application;
using EnterpriseWms.Contracts;
using EnterpriseWms.Domain;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseWms.Infrastructure;

public sealed class InventoryService : IInventoryService
{
    private readonly WarehouseDbContext _db;
    public InventoryService(WarehouseDbContext db) => _db = db;

    public async Task<PagedResult<InventoryDto>> GetInventoryAsync(int? warehouseId, string? keyword, string? category, bool warningOnly, int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = Query();
        if (warehouseId.HasValue) query = query.Where(x => x.WarehouseId == warehouseId.Value);
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var value = keyword.Trim();
            query = query.Where(x => x.Product.Sku.Contains(value) || x.Product.Name.Contains(value));
        }
        if (!string.IsNullOrWhiteSpace(category)) query = query.Where(x => x.Product.Category == category.Trim());
        if (warningOnly) query = query.Where(x => x.Quantity <= x.SafetyStock);
        var total = await query.CountAsync(cancellationToken);
        var safePage = Math.Max(1, page);
        var safeSize = Math.Clamp(pageSize, 1, 200);
        var items = await Project(query.OrderBy(x => x.Warehouse.Code).ThenBy(x => x.Product.Sku))
            .Skip((safePage - 1) * safeSize).Take(safeSize).ToListAsync(cancellationToken);
        return new PagedResult<InventoryDto> { Items = items, Page = safePage, PageSize = safeSize, TotalCount = total };
    }

    public Task<InventoryDto?> GetByCodesAsync(string warehouseCode, string sku, CancellationToken cancellationToken)
    {
        var wc = (warehouseCode ?? string.Empty).Trim();
        var productSku = (sku ?? string.Empty).Trim();
        return Project(Query().Where(x => x.Warehouse.Code == wc && x.Product.Sku == productSku)).SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<InventoryDto>> GetWarningsByWarehouseCodeAsync(string? warehouseCode, CancellationToken cancellationToken)
    {
        var query = Query().Where(x => x.Quantity <= x.SafetyStock);
        if (!string.IsNullOrWhiteSpace(warehouseCode)) query = query.Where(x => x.Warehouse.Code == warehouseCode.Trim());
        return await Project(query.OrderBy(x => x.Warehouse.Code).ThenBy(x => x.Product.Sku)).Take(500).ToListAsync(cancellationToken);
    }

    public async Task<bool> UpdateSafetyStockAsync(int inventoryId, decimal safetyStock, CancellationToken cancellationToken)
    {
        if (safetyStock < 0) throw new BusinessRuleException("validation.failed", "安全库存不能小于零。");
        var entity = await _db.Inventories.FindAsync(new object[] { inventoryId }, cancellationToken);
        if (entity == null) return false;
        entity.SafetyStock = safetyStock;
        entity.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private IQueryable<Inventory> Query() => _db.Inventories.AsNoTracking().Include(x => x.Warehouse).Include(x => x.Product);

    private static IQueryable<InventoryDto> Project(IQueryable<Inventory> query) => query.Select(x => new InventoryDto
    {
        Id = x.Id,
        WarehouseId = x.WarehouseId,
        WarehouseCode = x.Warehouse.Code,
        WarehouseName = x.Warehouse.Name,
        ProductId = x.ProductId,
        Sku = x.Product.Sku,
        ProductName = x.Product.Name,
        Category = x.Product.Category,
        Specification = x.Product.Specification,
        Unit = x.Product.Unit,
        Quantity = x.Quantity,
        SafetyStock = x.SafetyStock,
        IsLowStock = x.Quantity <= x.SafetyStock,
        UpdatedAtUtc = x.UpdatedAtUtc
    });
}

public sealed class ReportService : IReportService
{
    private readonly WarehouseDbContext _db;
    private readonly IInventoryService _inventory;
    public ReportService(WarehouseDbContext db, IInventoryService inventory) { _db = db; _inventory = inventory; }

    public async Task<DashboardDto> GetDashboardAsync(CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;
        var start = today.AddDays(-6);
        var result = new DashboardDto
        {
            ActiveProductCount = await _db.Products.CountAsync(x => x.IsActive, cancellationToken),
            ActiveWarehouseCount = await _db.Warehouses.CountAsync(x => x.IsActive, cancellationToken),
            LowStockCount = await _db.Inventories.CountAsync(x => x.Quantity <= x.SafetyStock, cancellationToken),
            TodayInboundCount = await _db.InboundOrders.CountAsync(x => x.PostedAtUtc >= today, cancellationToken),
            TodayOutboundCount = await _db.OutboundOrders.CountAsync(x => x.PostedAtUtc >= today, cancellationToken)
        };

        var inbound = await _db.InboundOrders.AsNoTracking().Where(x => x.PostedAtUtc >= start)
            .GroupBy(x => x.PostedAtUtc.Date).Select(x => new { Date = x.Key, Count = x.Count() }).ToListAsync(cancellationToken);
        var outbound = await _db.OutboundOrders.AsNoTracking().Where(x => x.PostedAtUtc >= start)
            .GroupBy(x => x.PostedAtUtc.Date).Select(x => new { Date = x.Key, Count = x.Count() }).ToListAsync(cancellationToken);
        for (var date = start; date <= today; date = date.AddDays(1))
            result.OrderTrend.Add(new ChartPointDto { Label = date.ToString("MM-dd"), Value = inbound.FirstOrDefault(x => x.Date == date)?.Count ?? 0, SecondaryValue = outbound.FirstOrDefault(x => x.Date == date)?.Count ?? 0 });

        result.WarningByWarehouse = await _db.Inventories.AsNoTracking().Where(x => x.Quantity <= x.SafetyStock)
            .GroupBy(x => x.Warehouse.Name).Select(x => new ChartPointDto { Label = x.Key, Value = x.Count() })
            .OrderByDescending(x => x.Value).ToListAsync(cancellationToken);
        return result;
    }

    public async Task<byte[]> CreateInventoryExcelAsync(int? warehouseId, bool warningOnly, CancellationToken cancellationToken)
    {
        var items = await GetInventoryDataAsync(warehouseId, warningOnly, cancellationToken);
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add(warningOnly ? "库存预警" : "实时库存");
        var headers = new[] { "仓库编码", "仓库名称", "SKU", "商品名称", "分类", "规格", "单位", "当前库存", "安全库存", "预警状态", "更新时间(UTC)" };
        for (var i = 0; i < headers.Length; i++) sheet.Cell(1, i + 1).Value = headers[i];
        var row = 2;
        foreach (var item in items)
        {
            sheet.Cell(row, 1).Value = item.WarehouseCode;
            sheet.Cell(row, 2).Value = item.WarehouseName;
            sheet.Cell(row, 3).Value = item.Sku;
            sheet.Cell(row, 4).Value = item.ProductName;
            sheet.Cell(row, 5).Value = item.Category;
            sheet.Cell(row, 6).Value = item.Specification;
            sheet.Cell(row, 7).Value = item.Unit;
            sheet.Cell(row, 8).Value = item.Quantity;
            sheet.Cell(row, 9).Value = item.SafetyStock;
            sheet.Cell(row, 10).Value = item.IsLowStock ? "库存不足" : "正常";
            sheet.Cell(row, 11).Value = item.UpdatedAtUtc;
            if (item.IsLowStock) sheet.Range(row, 1, row, headers.Length).Style.Fill.BackgroundColor = XLColor.LightPink;
            row++;
        }
        var range = sheet.Range(1, 1, Math.Max(1, row - 1), headers.Length);
        range.CreateTable();
        sheet.Row(1).Style.Font.Bold = true;
        sheet.SheetView.FreezeRows(1);
        sheet.Columns().AdjustToContents();
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<IReadOnlyList<InventoryDto>> GetInventoryDataAsync(int? warehouseId, bool warningOnly, CancellationToken cancellationToken)
    {
        var rows = new List<InventoryDto>();
        await _db.Database.OpenConnectionAsync(cancellationToken);
        try
        {
            await using var command = _db.Database.GetDbConnection().CreateCommand();
            command.CommandText = "dbo.usp_GetInventoryReport";
            command.CommandType = CommandType.StoredProcedure;
            var warehouse = command.CreateParameter(); warehouse.ParameterName = "@WarehouseId"; warehouse.Value = warehouseId.HasValue ? warehouseId.Value : DBNull.Value; command.Parameters.Add(warehouse);
            var warning = command.CreateParameter(); warning.ParameterName = "@WarningOnly"; warning.Value = warningOnly; command.Parameters.Add(warning);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                rows.Add(new InventoryDto
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")), WarehouseId = reader.GetInt32(reader.GetOrdinal("WarehouseId")), WarehouseCode = reader.GetString(reader.GetOrdinal("WarehouseCode")),
                    WarehouseName = reader.GetString(reader.GetOrdinal("WarehouseName")), ProductId = reader.GetInt32(reader.GetOrdinal("ProductId")), Sku = reader.GetString(reader.GetOrdinal("Sku")),
                    ProductName = reader.GetString(reader.GetOrdinal("ProductName")), Category = reader.GetString(reader.GetOrdinal("Category")), Specification = reader.GetString(reader.GetOrdinal("Specification")),
                    Unit = reader.GetString(reader.GetOrdinal("Unit")), Quantity = reader.GetDecimal(reader.GetOrdinal("Quantity")), SafetyStock = reader.GetDecimal(reader.GetOrdinal("SafetyStock")),
                    IsLowStock = reader.GetBoolean(reader.GetOrdinal("IsLowStock")), UpdatedAtUtc = reader.GetDateTime(reader.GetOrdinal("UpdatedAtUtc"))
                });
            }
        }
        finally { await _db.Database.CloseConnectionAsync(); }
        return rows;
    }
}
