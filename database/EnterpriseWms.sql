IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
CREATE SEQUENCE [StockOrderSequence] START WITH 1 INCREMENT BY 1 NO CYCLE;

CREATE TABLE [OperationLogs] (
    [Id] int NOT NULL IDENTITY,
    [UserId] int NULL,
    [Username] nvarchar(50) NOT NULL,
    [Module] nvarchar(80) NOT NULL,
    [Action] nvarchar(80) NOT NULL,
    [Target] nvarchar(300) NOT NULL,
    [Result] nvarchar(30) NOT NULL,
    [ElapsedMilliseconds] int NOT NULL,
    [IpAddress] nvarchar(64) NOT NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    CONSTRAINT [PK_OperationLogs] PRIMARY KEY ([Id])
);

CREATE TABLE [Products] (
    [Id] int NOT NULL IDENTITY,
    [Sku] nvarchar(50) NOT NULL,
    [Name] nvarchar(150) NOT NULL,
    [Category] nvarchar(80) NOT NULL,
    [Specification] nvarchar(150) NOT NULL,
    [Unit] nvarchar(20) NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_Products] PRIMARY KEY ([Id])
);

CREATE TABLE [StockMovements] (
    [Id] int NOT NULL IDENTITY,
    [Type] int NOT NULL,
    [SourceOrderNo] nvarchar(40) NOT NULL,
    [WarehouseId] int NOT NULL,
    [ProductId] int NOT NULL,
    [QuantityDelta] decimal(18,3) NOT NULL,
    [BeforeQuantity] decimal(18,3) NOT NULL,
    [AfterQuantity] decimal(18,3) NOT NULL,
    [OperatorId] int NOT NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    CONSTRAINT [PK_StockMovements] PRIMARY KEY ([Id])
);

CREATE TABLE [Users] (
    [Id] int NOT NULL IDENTITY,
    [Username] nvarchar(50) NOT NULL,
    [PasswordHash] nvarchar(500) NOT NULL,
    [DisplayName] nvarchar(100) NOT NULL,
    [Role] nvarchar(30) NOT NULL,
    [IsActive] bit NOT NULL,
    [LastLoginAtUtc] datetime2 NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
);

CREATE TABLE [Warehouses] (
    [Id] int NOT NULL IDENTITY,
    [Code] nvarchar(30) NOT NULL,
    [Name] nvarchar(100) NOT NULL,
    [Address] nvarchar(300) NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_Warehouses] PRIMARY KEY ([Id])
);

CREATE TABLE [InboundOrders] (
    [Id] int NOT NULL DEFAULT (NEXT VALUE FOR [StockOrderSequence]),
    [OrderNo] nvarchar(40) NOT NULL,
    [WarehouseId] int NOT NULL,
    [OperatorId] int NOT NULL,
    [Remark] nvarchar(500) NOT NULL,
    [PostedAtUtc] datetime2 NOT NULL,
    CONSTRAINT [PK_InboundOrders] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_InboundOrders_Users_OperatorId] FOREIGN KEY ([OperatorId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_InboundOrders_Warehouses_WarehouseId] FOREIGN KEY ([WarehouseId]) REFERENCES [Warehouses] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [Inventories] (
    [Id] int NOT NULL IDENTITY,
    [WarehouseId] int NOT NULL,
    [ProductId] int NOT NULL,
    [Quantity] decimal(18,3) NOT NULL,
    [SafetyStock] decimal(18,3) NOT NULL,
    [UpdatedAtUtc] datetime2 NOT NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_Inventories] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_Inventories_Quantity] CHECK ([Quantity] >= 0),
    CONSTRAINT [FK_Inventories_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Inventories_Warehouses_WarehouseId] FOREIGN KEY ([WarehouseId]) REFERENCES [Warehouses] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [OutboundOrders] (
    [Id] int NOT NULL DEFAULT (NEXT VALUE FOR [StockOrderSequence]),
    [OrderNo] nvarchar(40) NOT NULL,
    [WarehouseId] int NOT NULL,
    [OperatorId] int NOT NULL,
    [Remark] nvarchar(500) NOT NULL,
    [PostedAtUtc] datetime2 NOT NULL,
    CONSTRAINT [PK_OutboundOrders] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_OutboundOrders_Users_OperatorId] FOREIGN KEY ([OperatorId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_OutboundOrders_Warehouses_WarehouseId] FOREIGN KEY ([WarehouseId]) REFERENCES [Warehouses] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [InboundOrderItems] (
    [Id] int NOT NULL IDENTITY,
    [InboundOrderId] int NOT NULL,
    [ProductId] int NOT NULL,
    [Quantity] decimal(18,3) NOT NULL,
    [UnitCost] decimal(18,2) NULL,
    CONSTRAINT [PK_InboundOrderItems] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_InboundOrderItems_InboundOrders_InboundOrderId] FOREIGN KEY ([InboundOrderId]) REFERENCES [InboundOrders] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_InboundOrderItems_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [OutboundOrderItems] (
    [Id] int NOT NULL IDENTITY,
    [OutboundOrderId] int NOT NULL,
    [ProductId] int NOT NULL,
    [Quantity] decimal(18,3) NOT NULL,
    CONSTRAINT [PK_OutboundOrderItems] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_OutboundOrderItems_OutboundOrders_OutboundOrderId] FOREIGN KEY ([OutboundOrderId]) REFERENCES [OutboundOrders] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_OutboundOrderItems_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE NO ACTION
);

CREATE INDEX [IX_InboundOrderItems_InboundOrderId] ON [InboundOrderItems] ([InboundOrderId]);

CREATE INDEX [IX_InboundOrderItems_ProductId] ON [InboundOrderItems] ([ProductId]);

CREATE INDEX [IX_InboundOrders_OperatorId] ON [InboundOrders] ([OperatorId]);

CREATE UNIQUE INDEX [IX_InboundOrders_OrderNo] ON [InboundOrders] ([OrderNo]);

CREATE INDEX [IX_InboundOrders_PostedAtUtc] ON [InboundOrders] ([PostedAtUtc]);

CREATE INDEX [IX_InboundOrders_WarehouseId] ON [InboundOrders] ([WarehouseId]);

CREATE INDEX [IX_Inventories_ProductId] ON [Inventories] ([ProductId]);

CREATE UNIQUE INDEX [IX_Inventories_WarehouseId_ProductId] ON [Inventories] ([WarehouseId], [ProductId]);

CREATE INDEX [IX_Inventories_WarehouseId_Quantity_SafetyStock] ON [Inventories] ([WarehouseId], [Quantity], [SafetyStock]);

CREATE INDEX [IX_OperationLogs_CreatedAtUtc] ON [OperationLogs] ([CreatedAtUtc]);

CREATE INDEX [IX_OutboundOrderItems_OutboundOrderId] ON [OutboundOrderItems] ([OutboundOrderId]);

CREATE INDEX [IX_OutboundOrderItems_ProductId] ON [OutboundOrderItems] ([ProductId]);

CREATE INDEX [IX_OutboundOrders_OperatorId] ON [OutboundOrders] ([OperatorId]);

CREATE UNIQUE INDEX [IX_OutboundOrders_OrderNo] ON [OutboundOrders] ([OrderNo]);

CREATE INDEX [IX_OutboundOrders_PostedAtUtc] ON [OutboundOrders] ([PostedAtUtc]);

CREATE INDEX [IX_OutboundOrders_WarehouseId] ON [OutboundOrders] ([WarehouseId]);

CREATE INDEX [IX_Products_Name_Category] ON [Products] ([Name], [Category]);

CREATE UNIQUE INDEX [IX_Products_Sku] ON [Products] ([Sku]);

CREATE INDEX [IX_StockMovements_SourceOrderNo] ON [StockMovements] ([SourceOrderNo]);

CREATE INDEX [IX_StockMovements_WarehouseId_ProductId_CreatedAtUtc] ON [StockMovements] ([WarehouseId], [ProductId], [CreatedAtUtc]);

CREATE UNIQUE INDEX [IX_Users_Username] ON [Users] ([Username]);

CREATE UNIQUE INDEX [IX_Warehouses_Code] ON [Warehouses] ([Code]);

CREATE VIEW dbo.vw_CurrentInventory AS
SELECT i.Id, i.WarehouseId, w.Code AS WarehouseCode, w.Name AS WarehouseName,
       i.ProductId, p.Sku, p.Name AS ProductName, p.Category, p.Specification, p.Unit,
       i.Quantity, i.SafetyStock,
       CAST(CASE WHEN i.Quantity <= i.SafetyStock THEN 1 ELSE 0 END AS bit) AS IsLowStock,
       i.UpdatedAtUtc
FROM dbo.Inventories i
INNER JOIN dbo.Warehouses w ON w.Id = i.WarehouseId
INNER JOIN dbo.Products p ON p.Id = i.ProductId;

CREATE VIEW dbo.vw_InventoryWarnings AS
SELECT * FROM dbo.vw_CurrentInventory WHERE IsLowStock = 1;

CREATE TYPE dbo.StockOrderItemType AS TABLE
(
    ProductId int NOT NULL PRIMARY KEY,
    Quantity decimal(18,3) NOT NULL,
    UnitCost decimal(18,2) NULL
);

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

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260811072731_InitialCreate', N'10.0.0');

COMMIT;
GO

