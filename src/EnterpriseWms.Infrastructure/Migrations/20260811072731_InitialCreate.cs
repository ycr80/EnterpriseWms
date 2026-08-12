using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnterpriseWms.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence(
                name: "StockOrderSequence");

            migrationBuilder.CreateTable(
                name: "OperationLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    Username = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Module = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Action = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Target = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Result = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ElapsedMilliseconds = table.Column<int>(type: "int", nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OperationLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Sku = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Specification = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StockMovements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Type = table.Column<int>(type: "int", nullable: false),
                    SourceOrderNo = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    QuantityDelta = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    BeforeQuantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    AfterQuantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    OperatorId = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockMovements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Role = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    LastLoginAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Warehouses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Warehouses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InboundOrders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false, defaultValueSql: "NEXT VALUE FOR [StockOrderSequence]"),
                    OrderNo = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    OperatorId = table.Column<int>(type: "int", nullable: false),
                    Remark = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    PostedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InboundOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InboundOrders_Users_OperatorId",
                        column: x => x.OperatorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InboundOrders_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Inventories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    SafetyStock = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Inventories", x => x.Id);
                    table.CheckConstraint("CK_Inventories_Quantity", "[Quantity] >= 0");
                    table.ForeignKey(
                        name: "FK_Inventories_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Inventories_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OutboundOrders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false, defaultValueSql: "NEXT VALUE FOR [StockOrderSequence]"),
                    OrderNo = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    OperatorId = table.Column<int>(type: "int", nullable: false),
                    Remark = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    PostedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboundOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OutboundOrders_Users_OperatorId",
                        column: x => x.OperatorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OutboundOrders_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InboundOrderItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InboundOrderId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    UnitCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InboundOrderItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InboundOrderItems_InboundOrders_InboundOrderId",
                        column: x => x.InboundOrderId,
                        principalTable: "InboundOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InboundOrderItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OutboundOrderItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OutboundOrderId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboundOrderItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OutboundOrderItems_OutboundOrders_OutboundOrderId",
                        column: x => x.OutboundOrderId,
                        principalTable: "OutboundOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OutboundOrderItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InboundOrderItems_InboundOrderId",
                table: "InboundOrderItems",
                column: "InboundOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_InboundOrderItems_ProductId",
                table: "InboundOrderItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_InboundOrders_OperatorId",
                table: "InboundOrders",
                column: "OperatorId");

            migrationBuilder.CreateIndex(
                name: "IX_InboundOrders_OrderNo",
                table: "InboundOrders",
                column: "OrderNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InboundOrders_PostedAtUtc",
                table: "InboundOrders",
                column: "PostedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_InboundOrders_WarehouseId",
                table: "InboundOrders",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_Inventories_ProductId",
                table: "Inventories",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Inventories_WarehouseId_ProductId",
                table: "Inventories",
                columns: new[] { "WarehouseId", "ProductId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Inventories_WarehouseId_Quantity_SafetyStock",
                table: "Inventories",
                columns: new[] { "WarehouseId", "Quantity", "SafetyStock" });

            migrationBuilder.CreateIndex(
                name: "IX_OperationLogs_CreatedAtUtc",
                table: "OperationLogs",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_OutboundOrderItems_OutboundOrderId",
                table: "OutboundOrderItems",
                column: "OutboundOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_OutboundOrderItems_ProductId",
                table: "OutboundOrderItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_OutboundOrders_OperatorId",
                table: "OutboundOrders",
                column: "OperatorId");

            migrationBuilder.CreateIndex(
                name: "IX_OutboundOrders_OrderNo",
                table: "OutboundOrders",
                column: "OrderNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OutboundOrders_PostedAtUtc",
                table: "OutboundOrders",
                column: "PostedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_OutboundOrders_WarehouseId",
                table: "OutboundOrders",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_Name_Category",
                table: "Products",
                columns: new[] { "Name", "Category" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_Sku",
                table: "Products",
                column: "Sku",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_SourceOrderNo",
                table: "StockMovements",
                column: "SourceOrderNo");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_WarehouseId_ProductId_CreatedAtUtc",
                table: "StockMovements",
                columns: new[] { "WarehouseId", "ProductId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Warehouses_Code",
                table: "Warehouses",
                column: "Code",
                unique: true);

            migrationBuilder.Sql("""
                CREATE VIEW dbo.vw_CurrentInventory AS
                SELECT i.Id, i.WarehouseId, w.Code AS WarehouseCode, w.Name AS WarehouseName,
                       i.ProductId, p.Sku, p.Name AS ProductName, p.Category, p.Specification, p.Unit,
                       i.Quantity, i.SafetyStock,
                       CAST(CASE WHEN i.Quantity <= i.SafetyStock THEN 1 ELSE 0 END AS bit) AS IsLowStock,
                       i.UpdatedAtUtc
                FROM dbo.Inventories i
                INNER JOIN dbo.Warehouses w ON w.Id = i.WarehouseId
                INNER JOIN dbo.Products p ON p.Id = i.ProductId;
                """);

            migrationBuilder.Sql("""
                CREATE VIEW dbo.vw_InventoryWarnings AS
                SELECT * FROM dbo.vw_CurrentInventory WHERE IsLowStock = 1;
                """);

            migrationBuilder.Sql("""
                CREATE TYPE dbo.StockOrderItemType AS TABLE
                (
                    ProductId int NOT NULL PRIMARY KEY,
                    Quantity decimal(18,3) NOT NULL,
                    UnitCost decimal(18,2) NULL
                );
                """);

            migrationBuilder.Sql("""
                CREATE PROCEDURE dbo.usp_PostInboundOrder
                    @OrderNo nvarchar(40),
                    @WarehouseId int,
                    @OperatorId int,
                    @Remark nvarchar(500),
                    @Items dbo.StockOrderItemType READONLY,
                    @OrderId int OUTPUT
                AS
                BEGIN
                    SET NOCOUNT ON;
                    SET XACT_ABORT ON;
                    IF NOT EXISTS (SELECT 1 FROM @Items) THROW 51002, N'入库单至少包含一个商品。', 1;
                    IF EXISTS (SELECT 1 FROM @Items WHERE Quantity <= 0) THROW 51002, N'入库数量必须大于零。', 1;
                    BEGIN TRY
                        BEGIN TRANSACTION;
                        IF NOT EXISTS (SELECT 1 FROM dbo.Warehouses WITH (UPDLOCK, HOLDLOCK) WHERE Id = @WarehouseId AND IsActive = 1)
                            THROW 51003, N'仓库不存在或已停用。', 1;
                        IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Id = @OperatorId AND IsActive = 1)
                            THROW 51003, N'操作用户不存在或已停用。', 1;
                        IF EXISTS (SELECT 1 FROM @Items i LEFT JOIN dbo.Products p ON p.Id = i.ProductId AND p.IsActive = 1 WHERE p.Id IS NULL)
                            THROW 51003, N'入库单包含不存在或已停用的商品。', 1;

                        SET @OrderId = NEXT VALUE FOR dbo.StockOrderSequence;
                        INSERT dbo.InboundOrders(Id, OrderNo, WarehouseId, OperatorId, Remark, PostedAtUtc)
                        VALUES(@OrderId, @OrderNo, @WarehouseId, @OperatorId, ISNULL(@Remark, N''), SYSUTCDATETIME());
                        INSERT dbo.InboundOrderItems(InboundOrderId, ProductId, Quantity, UnitCost)
                        SELECT @OrderId, ProductId, Quantity, UnitCost FROM @Items;

                        INSERT dbo.Inventories(WarehouseId, ProductId, Quantity, SafetyStock, UpdatedAtUtc)
                        SELECT @WarehouseId, i.ProductId, 0, 0, SYSUTCDATETIME()
                        FROM @Items i
                        WHERE NOT EXISTS
                        (
                            SELECT 1 FROM dbo.Inventories currentStock WITH (UPDLOCK, HOLDLOCK)
                            WHERE currentStock.WarehouseId = @WarehouseId AND currentStock.ProductId = i.ProductId
                        );

                        DECLARE @Before TABLE(ProductId int PRIMARY KEY, BeforeQuantity decimal(18,3));
                        INSERT @Before(ProductId, BeforeQuantity)
                        SELECT TOP (2147483647) stock.ProductId, stock.Quantity
                        FROM dbo.Inventories stock WITH (UPDLOCK, HOLDLOCK)
                        INNER JOIN @Items i ON i.ProductId = stock.ProductId
                        WHERE stock.WarehouseId = @WarehouseId;

                        UPDATE stock
                        SET Quantity = stock.Quantity + i.Quantity, UpdatedAtUtc = SYSUTCDATETIME()
                        FROM dbo.Inventories stock
                        INNER JOIN @Items i ON i.ProductId = stock.ProductId
                        WHERE stock.WarehouseId = @WarehouseId;

                        INSERT dbo.StockMovements(Type, SourceOrderNo, WarehouseId, ProductId, QuantityDelta, BeforeQuantity, AfterQuantity, OperatorId, CreatedAtUtc)
                        SELECT 1, @OrderNo, @WarehouseId, i.ProductId, i.Quantity, b.BeforeQuantity, b.BeforeQuantity + i.Quantity, @OperatorId, SYSUTCDATETIME()
                        FROM @Items i INNER JOIN @Before b ON b.ProductId = i.ProductId;
                        COMMIT TRANSACTION;
                    END TRY
                    BEGIN CATCH
                        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
                        THROW;
                    END CATCH
                END;
                """);

            migrationBuilder.Sql("""
                CREATE PROCEDURE dbo.usp_PostOutboundOrder
                    @OrderNo nvarchar(40),
                    @WarehouseId int,
                    @OperatorId int,
                    @Remark nvarchar(500),
                    @Items dbo.StockOrderItemType READONLY,
                    @OrderId int OUTPUT
                AS
                BEGIN
                    SET NOCOUNT ON;
                    SET XACT_ABORT ON;
                    IF NOT EXISTS (SELECT 1 FROM @Items) THROW 51002, N'出库单至少包含一个商品。', 1;
                    IF EXISTS (SELECT 1 FROM @Items WHERE Quantity <= 0) THROW 51002, N'出库数量必须大于零。', 1;
                    BEGIN TRY
                        BEGIN TRANSACTION;
                        IF NOT EXISTS (SELECT 1 FROM dbo.Warehouses WITH (UPDLOCK, HOLDLOCK) WHERE Id = @WarehouseId AND IsActive = 1)
                            THROW 51003, N'仓库不存在或已停用。', 1;
                        IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Id = @OperatorId AND IsActive = 1)
                            THROW 51003, N'操作用户不存在或已停用。', 1;
                        IF EXISTS (SELECT 1 FROM @Items i LEFT JOIN dbo.Products p ON p.Id = i.ProductId AND p.IsActive = 1 WHERE p.Id IS NULL)
                            THROW 51003, N'出库单包含不存在或已停用的商品。', 1;

                        DECLARE @Before TABLE(ProductId int PRIMARY KEY, BeforeQuantity decimal(18,3));
                        INSERT @Before(ProductId, BeforeQuantity)
                        SELECT TOP (2147483647) stock.ProductId, stock.Quantity
                        FROM dbo.Inventories stock WITH (UPDLOCK, HOLDLOCK)
                        INNER JOIN @Items i ON i.ProductId = stock.ProductId
                        WHERE stock.WarehouseId = @WarehouseId
                        ORDER BY stock.ProductId;

                        IF EXISTS
                        (
                            SELECT 1 FROM @Items i
                            LEFT JOIN @Before b ON b.ProductId = i.ProductId
                            WHERE b.ProductId IS NULL OR b.BeforeQuantity < i.Quantity
                        )
                        BEGIN
                            DECLARE @ErrorMessage nvarchar(2048);
                            SELECT TOP (1) @ErrorMessage = N'库存不足：' + p.Sku + N' ' + p.Name
                            FROM @Items i
                            INNER JOIN dbo.Products p ON p.Id = i.ProductId
                            LEFT JOIN @Before b ON b.ProductId = i.ProductId
                            WHERE b.ProductId IS NULL OR b.BeforeQuantity < i.Quantity
                            ORDER BY i.ProductId;
                            THROW 51001, @ErrorMessage, 1;
                        END;

                        SET @OrderId = NEXT VALUE FOR dbo.StockOrderSequence;
                        INSERT dbo.OutboundOrders(Id, OrderNo, WarehouseId, OperatorId, Remark, PostedAtUtc)
                        VALUES(@OrderId, @OrderNo, @WarehouseId, @OperatorId, ISNULL(@Remark, N''), SYSUTCDATETIME());
                        INSERT dbo.OutboundOrderItems(OutboundOrderId, ProductId, Quantity)
                        SELECT @OrderId, ProductId, Quantity FROM @Items;

                        UPDATE stock
                        SET Quantity = stock.Quantity - i.Quantity, UpdatedAtUtc = SYSUTCDATETIME()
                        FROM dbo.Inventories stock
                        INNER JOIN @Items i ON i.ProductId = stock.ProductId
                        WHERE stock.WarehouseId = @WarehouseId;

                        INSERT dbo.StockMovements(Type, SourceOrderNo, WarehouseId, ProductId, QuantityDelta, BeforeQuantity, AfterQuantity, OperatorId, CreatedAtUtc)
                        SELECT 2, @OrderNo, @WarehouseId, i.ProductId, -i.Quantity, b.BeforeQuantity, b.BeforeQuantity - i.Quantity, @OperatorId, SYSUTCDATETIME()
                        FROM @Items i INNER JOIN @Before b ON b.ProductId = i.ProductId;
                        COMMIT TRANSACTION;
                    END TRY
                    BEGIN CATCH
                        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
                        THROW;
                    END CATCH
                END;
                """);

            migrationBuilder.Sql("""
                CREATE PROCEDURE dbo.usp_GetInventoryReport
                    @WarehouseId int = NULL,
                    @WarningOnly bit = 0
                AS
                BEGIN
                    SET NOCOUNT ON;
                    SELECT * FROM dbo.vw_CurrentInventory
                    WHERE (@WarehouseId IS NULL OR WarehouseId = @WarehouseId)
                      AND (@WarningOnly = 0 OR IsLowStock = 1)
                    ORDER BY WarehouseCode, Sku;
                END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.usp_GetInventoryReport;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.usp_PostOutboundOrder;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.usp_PostInboundOrder;");
            migrationBuilder.Sql("DROP VIEW IF EXISTS dbo.vw_InventoryWarnings;");
            migrationBuilder.Sql("DROP VIEW IF EXISTS dbo.vw_CurrentInventory;");
            migrationBuilder.Sql("DROP TYPE IF EXISTS dbo.StockOrderItemType;");

            migrationBuilder.DropTable(
                name: "InboundOrderItems");

            migrationBuilder.DropTable(
                name: "Inventories");

            migrationBuilder.DropTable(
                name: "OperationLogs");

            migrationBuilder.DropTable(
                name: "OutboundOrderItems");

            migrationBuilder.DropTable(
                name: "StockMovements");

            migrationBuilder.DropTable(
                name: "InboundOrders");

            migrationBuilder.DropTable(
                name: "OutboundOrders");

            migrationBuilder.DropTable(
                name: "Products");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Warehouses");

            migrationBuilder.DropSequence(
                name: "StockOrderSequence");
        }
    }
}
