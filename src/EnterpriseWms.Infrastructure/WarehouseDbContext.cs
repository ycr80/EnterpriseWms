using EnterpriseWms.Domain;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseWms.Infrastructure;

public sealed class WarehouseDbContext : DbContext
{
    public WarehouseDbContext(DbContextOptions<WarehouseDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<Inventory> Inventories => Set<Inventory>();
    public DbSet<InboundOrder> InboundOrders => Set<InboundOrder>();
    public DbSet<InboundOrderItem> InboundOrderItems => Set<InboundOrderItem>();
    public DbSet<OutboundOrder> OutboundOrders => Set<OutboundOrder>();
    public DbSet<OutboundOrderItem> OutboundOrderItems => Set<OutboundOrderItem>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<OperationLog> OperationLogs => Set<OperationLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(x => x.Username).IsUnique();
            entity.Property(x => x.Username).HasMaxLength(50).IsRequired();
            entity.Property(x => x.PasswordHash).HasMaxLength(500).IsRequired();
            entity.Property(x => x.DisplayName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Role).HasMaxLength(30).IsRequired();
            entity.Property(x => x.RowVersion).IsRowVersion();
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasIndex(x => x.Sku).IsUnique();
            entity.HasIndex(x => new { x.Name, x.Category });
            entity.Property(x => x.Sku).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(150).IsRequired();
            entity.Property(x => x.Category).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Specification).HasMaxLength(150);
            entity.Property(x => x.Unit).HasMaxLength(20).IsRequired();
            entity.Property(x => x.RowVersion).IsRowVersion();
        });

        modelBuilder.Entity<Warehouse>(entity =>
        {
            entity.HasIndex(x => x.Code).IsUnique();
            entity.Property(x => x.Code).HasMaxLength(30).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Address).HasMaxLength(300);
            entity.Property(x => x.RowVersion).IsRowVersion();
        });

        modelBuilder.Entity<Inventory>(entity =>
        {
            entity.HasIndex(x => new { x.WarehouseId, x.ProductId }).IsUnique();
            entity.HasIndex(x => new { x.WarehouseId, x.Quantity, x.SafetyStock });
            entity.Property(x => x.Quantity).HasPrecision(18, 3);
            entity.Property(x => x.SafetyStock).HasPrecision(18, 3);
            entity.Property(x => x.RowVersion).IsRowVersion();
            entity.ToTable(t => t.HasCheckConstraint("CK_Inventories_Quantity", "[Quantity] >= 0"));
            entity.HasOne(x => x.Warehouse).WithMany(x => x.Inventories).HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Product).WithMany(x => x.Inventories).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<StockOrder>().UseTpcMappingStrategy();
        modelBuilder.Entity<InboundOrder>(entity =>
        {
            entity.ToTable("InboundOrders");
            ConfigureOrder(entity);
            entity.HasMany(x => x.Items).WithOne(x => x.Order).HasForeignKey(x => x.InboundOrderId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<OutboundOrder>(entity =>
        {
            entity.ToTable("OutboundOrders");
            ConfigureOrder(entity);
            entity.HasMany(x => x.Items).WithOne(x => x.Order).HasForeignKey(x => x.OutboundOrderId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<InboundOrderItem>(entity =>
        {
            entity.Property(x => x.Quantity).HasPrecision(18, 3);
            entity.Property(x => x.UnitCost).HasPrecision(18, 2);
            entity.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<OutboundOrderItem>(entity =>
        {
            entity.Property(x => x.Quantity).HasPrecision(18, 3);
            entity.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<StockMovement>(entity =>
        {
            entity.HasIndex(x => new { x.WarehouseId, x.ProductId, x.CreatedAtUtc });
            entity.HasIndex(x => x.SourceOrderNo);
            entity.Property(x => x.SourceOrderNo).HasMaxLength(40).IsRequired();
            entity.Property(x => x.QuantityDelta).HasPrecision(18, 3);
            entity.Property(x => x.BeforeQuantity).HasPrecision(18, 3);
            entity.Property(x => x.AfterQuantity).HasPrecision(18, 3);
        });

        modelBuilder.Entity<OperationLog>(entity =>
        {
            entity.HasIndex(x => x.CreatedAtUtc);
            entity.Property(x => x.Username).HasMaxLength(50);
            entity.Property(x => x.Module).HasMaxLength(80);
            entity.Property(x => x.Action).HasMaxLength(80);
            entity.Property(x => x.Target).HasMaxLength(300);
            entity.Property(x => x.Result).HasMaxLength(30);
            entity.Property(x => x.IpAddress).HasMaxLength(64);
        });
    }

    private static void ConfigureOrder<T>(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<T> entity)
        where T : StockOrder
    {
        entity.HasIndex(x => x.OrderNo).IsUnique();
        entity.HasIndex(x => x.PostedAtUtc);
        entity.Property(x => x.OrderNo).HasMaxLength(40).IsRequired();
        entity.Property(x => x.Remark).HasMaxLength(500);
        entity.HasOne(x => x.Warehouse).WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.Operator).WithMany().HasForeignKey(x => x.OperatorId).OnDelete(DeleteBehavior.Restrict);
    }
}
