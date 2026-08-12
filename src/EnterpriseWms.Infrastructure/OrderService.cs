using System.Data;
using EnterpriseWms.Application;
using EnterpriseWms.Contracts;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseWms.Infrastructure;

public sealed class OrderService : IOrderService
{
    private readonly WarehouseDbContext _db;
    public OrderService(WarehouseDbContext db) => _db = db;

    public Task<StockOrderDto> PostInboundAsync(CreateStockOrderRequest request, int operatorId, CancellationToken cancellationToken) =>
        PostAsync("usp_PostInboundOrder", "IN", "inbound", request, operatorId, cancellationToken);

    public Task<StockOrderDto> PostOutboundAsync(CreateStockOrderRequest request, int operatorId, CancellationToken cancellationToken) =>
        PostAsync("usp_PostOutboundOrder", "OUT", "outbound", request, operatorId, cancellationToken);

    private async Task<StockOrderDto> PostAsync(string procedure, string prefix, string type, CreateStockOrderRequest request, int operatorId, CancellationToken cancellationToken)
    {
        OrderValidator.Validate(request);
        var warehouseActive = await _db.Warehouses.AnyAsync(x => x.Id == request.WarehouseId && x.IsActive, cancellationToken);
        if (!warehouseActive) throw new BusinessRuleException("warehouse.inactive", "仓库不存在或已停用。");
        var productIds = request.Items.Select(x => x.ProductId).Distinct().ToList();
        if (await _db.Products.CountAsync(x => productIds.Contains(x.Id) && x.IsActive, cancellationToken) != productIds.Count)
            throw new BusinessRuleException("product.inactive", "单据中包含不存在或已停用的商品。");

        var orderNo = $"{prefix}{DateTime.UtcNow:yyyyMMddHHmmssfff}{Random.Shared.Next(100, 999)}";
        var table = new DataTable();
        table.Columns.Add("ProductId", typeof(int));
        table.Columns.Add("Quantity", typeof(decimal));
        table.Columns.Add("UnitCost", typeof(decimal));
        foreach (var item in request.Items.OrderBy(x => x.ProductId))
            table.Rows.Add(item.ProductId, item.Quantity, item.UnitCost.HasValue ? item.UnitCost.Value : DBNull.Value);

        await _db.Database.OpenConnectionAsync(cancellationToken);
        try
        {
            await using var command = _db.Database.GetDbConnection().CreateCommand();
            command.CommandText = $"dbo.{procedure}";
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.Add(new SqlParameter("@OrderNo", SqlDbType.NVarChar, 40) { Value = orderNo });
            command.Parameters.Add(new SqlParameter("@WarehouseId", SqlDbType.Int) { Value = request.WarehouseId });
            command.Parameters.Add(new SqlParameter("@OperatorId", SqlDbType.Int) { Value = operatorId });
            command.Parameters.Add(new SqlParameter("@Remark", SqlDbType.NVarChar, 500) { Value = request.Remark ?? string.Empty });
            command.Parameters.Add(new SqlParameter("@Items", SqlDbType.Structured) { TypeName = "dbo.StockOrderItemType", Value = table });
            var output = new SqlParameter("@OrderId", SqlDbType.Int) { Direction = ParameterDirection.Output };
            command.Parameters.Add(output);
            await command.ExecuteNonQueryAsync(cancellationToken);
            var id = Convert.ToInt32(output.Value);
            _db.ChangeTracker.Clear();
            return (await GetOrderAsync(type, id, cancellationToken))!;
        }
        catch (SqlException exception) when (exception.Number == 51001)
        {
            throw new BusinessRuleException("inventory.insufficient", exception.Message);
        }
        catch (SqlException exception) when (exception.Number is 51002 or 51003)
        {
            throw new BusinessRuleException("validation.failed", exception.Message);
        }
        finally
        {
            await _db.Database.CloseConnectionAsync();
        }
    }

    public async Task<PagedResult<StockOrderDto>> GetOrdersAsync(string type, int? warehouseId, int page, int pageSize, CancellationToken cancellationToken)
    {
        var safePage = Math.Max(1, page);
        var safeSize = Math.Clamp(pageSize, 1, 100);
        if (string.Equals(type, "inbound", StringComparison.OrdinalIgnoreCase))
        {
            var query = _db.InboundOrders.AsNoTracking().AsQueryable();
            if (warehouseId.HasValue) query = query.Where(x => x.WarehouseId == warehouseId.Value);
            var total = await query.CountAsync(cancellationToken);
            var rows = await query.OrderByDescending(x => x.PostedAtUtc).Skip((safePage - 1) * safeSize).Take(safeSize)
                .Select(x => new StockOrderDto { Id = x.Id, OrderNo = x.OrderNo, Type = "入库", WarehouseId = x.WarehouseId, WarehouseName = x.Warehouse.Name, OperatorName = x.Operator.DisplayName, Remark = x.Remark, PostedAtUtc = x.PostedAtUtc })
                .ToListAsync(cancellationToken);
            return new PagedResult<StockOrderDto> { Items = rows, Page = safePage, PageSize = safeSize, TotalCount = total };
        }
        else
        {
            var query = _db.OutboundOrders.AsNoTracking().AsQueryable();
            if (warehouseId.HasValue) query = query.Where(x => x.WarehouseId == warehouseId.Value);
            var total = await query.CountAsync(cancellationToken);
            var rows = await query.OrderByDescending(x => x.PostedAtUtc).Skip((safePage - 1) * safeSize).Take(safeSize)
                .Select(x => new StockOrderDto { Id = x.Id, OrderNo = x.OrderNo, Type = "出库", WarehouseId = x.WarehouseId, WarehouseName = x.Warehouse.Name, OperatorName = x.Operator.DisplayName, Remark = x.Remark, PostedAtUtc = x.PostedAtUtc })
                .ToListAsync(cancellationToken);
            return new PagedResult<StockOrderDto> { Items = rows, Page = safePage, PageSize = safeSize, TotalCount = total };
        }
    }

    public async Task<StockOrderDto?> GetOrderAsync(string type, int id, CancellationToken cancellationToken)
    {
        if (string.Equals(type, "inbound", StringComparison.OrdinalIgnoreCase))
        {
            var order = await _db.InboundOrders.AsNoTracking().Include(x => x.Warehouse).Include(x => x.Operator).Include(x => x.Items).ThenInclude(x => x.Product)
                .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (order == null) return null;
            return new StockOrderDto
            {
                Id = order.Id, OrderNo = order.OrderNo, Type = "入库", WarehouseId = order.WarehouseId, WarehouseName = order.Warehouse.Name,
                OperatorName = order.Operator.DisplayName, Remark = order.Remark, PostedAtUtc = order.PostedAtUtc,
                Items = order.Items.OrderBy(x => x.Product.Sku).Select(x => new StockOrderLineDto { ProductId = x.ProductId, Sku = x.Product.Sku, ProductName = x.Product.Name, Quantity = x.Quantity, UnitCost = x.UnitCost }).ToList()
            };
        }
        var outbound = await _db.OutboundOrders.AsNoTracking().Include(x => x.Warehouse).Include(x => x.Operator).Include(x => x.Items).ThenInclude(x => x.Product)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (outbound == null) return null;
        return new StockOrderDto
        {
            Id = outbound.Id, OrderNo = outbound.OrderNo, Type = "出库", WarehouseId = outbound.WarehouseId, WarehouseName = outbound.Warehouse.Name,
            OperatorName = outbound.Operator.DisplayName, Remark = outbound.Remark, PostedAtUtc = outbound.PostedAtUtc,
            Items = outbound.Items.OrderBy(x => x.Product.Sku).Select(x => new StockOrderLineDto { ProductId = x.ProductId, Sku = x.Product.Sku, ProductName = x.Product.Name, Quantity = x.Quantity }).ToList()
        };
    }
}
