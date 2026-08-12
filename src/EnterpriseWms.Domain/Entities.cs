namespace EnterpriseWms.Domain;

public abstract class Entity
{
    public int Id { get; set; }
}

public static class Roles
{
    public const string Admin = "Admin";
    public const string Operator = "Operator";
    public const string Viewer = "Viewer";
    public const string All = Admin + "," + Operator + "," + Viewer;
    public const string CanOperate = Admin + "," + Operator;
}

public enum StockMovementType
{
    Inbound = 1,
    Outbound = 2
}

public sealed class User : Entity
{
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Role { get; set; } = Roles.Viewer;
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}

public sealed class Product : Entity
{
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Specification { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    public ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();
}

public sealed class Warehouse : Entity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    public ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();
}

public sealed class Inventory : Entity
{
    public int WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = null!;
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public decimal Quantity { get; set; }
    public decimal SafetyStock { get; set; }
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}

public abstract class StockOrder : Entity
{
    public string OrderNo { get; set; } = string.Empty;
    public int WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = null!;
    public int OperatorId { get; set; }
    public User Operator { get; set; } = null!;
    public string Remark { get; set; } = string.Empty;
    public DateTime PostedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class InboundOrder : StockOrder
{
    public ICollection<InboundOrderItem> Items { get; set; } = new List<InboundOrderItem>();
}

public sealed class InboundOrderItem : Entity
{
    public int InboundOrderId { get; set; }
    public InboundOrder Order { get; set; } = null!;
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public decimal Quantity { get; set; }
    public decimal? UnitCost { get; set; }
}

public sealed class OutboundOrder : StockOrder
{
    public ICollection<OutboundOrderItem> Items { get; set; } = new List<OutboundOrderItem>();
}

public sealed class OutboundOrderItem : Entity
{
    public int OutboundOrderId { get; set; }
    public OutboundOrder Order { get; set; } = null!;
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public decimal Quantity { get; set; }
}

public sealed class StockMovement : Entity
{
    public StockMovementType Type { get; set; }
    public string SourceOrderNo { get; set; } = string.Empty;
    public int WarehouseId { get; set; }
    public int ProductId { get; set; }
    public decimal QuantityDelta { get; set; }
    public decimal BeforeQuantity { get; set; }
    public decimal AfterQuantity { get; set; }
    public int OperatorId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class OperationLog : Entity
{
    public int? UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Target { get; set; } = string.Empty;
    public string Result { get; set; } = string.Empty;
    public int ElapsedMilliseconds { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
