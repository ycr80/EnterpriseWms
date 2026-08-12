# 架构与数据库设计

## 分层

```mermaid
flowchart LR
    UI["WinForms .NET Framework 4.8"] -->|"JWT + REST/JSON"| API["ASP.NET Core Web API"]
    UI -->|"BasicHttpBinding SOAP"| SOAP["CoreWCF / WSDL"]
    API --> APP["Application 服务与规则"]
    SOAP --> APP
    APP --> INFRA["Infrastructure / EF Core"]
    INFRA --> SQL["SQL Server LocalDB"]
    SQL --> SP["事务存储过程与视图"]
```

- `Contracts` 目标为 .NET Standard 2.0，使现代 API 与 net48 客户端共享 REST DTO。
- `Domain` 保存领域实体和固定角色。
- `Application` 定义用例接口、业务异常与订单校验。
- `Infrastructure` 实现 EF Core、主数据、库存、订单、报表及种子数据。
- `Api` 负责 JWT、Policy、统一 ProblemDetails、REST、SOAP 和操作日志。

## ER 模型

```mermaid
erDiagram
    USER ||--o{ INBOUND_ORDER : posts
    USER ||--o{ OUTBOUND_ORDER : posts
    WAREHOUSE ||--o{ INVENTORY : contains
    PRODUCT ||--o{ INVENTORY : stocked_as
    WAREHOUSE ||--o{ INBOUND_ORDER : receives
    WAREHOUSE ||--o{ OUTBOUND_ORDER : ships
    INBOUND_ORDER ||--|{ INBOUND_ORDER_ITEM : contains
    OUTBOUND_ORDER ||--|{ OUTBOUND_ORDER_ITEM : contains
    PRODUCT ||--o{ INBOUND_ORDER_ITEM : references
    PRODUCT ||--o{ OUTBOUND_ORDER_ITEM : references
    INVENTORY ||--o{ STOCK_MOVEMENT : produces
```

商品 SKU、仓库编码、用户名以及“仓库 + 商品”库存位均有唯一索引。商品和仓库使用 `IsActive` 停用，历史单据永不级联删除。

## 出库一致性

```mermaid
sequenceDiagram
    participant C as WinForms
    participant A as REST API
    participant S as usp_PostOutboundOrder
    participant D as SQL Server
    C->>A: POST 多明细出库单
    A->>A: JWT、角色、参数和商品状态校验
    A->>S: TVP 明细 + 仓库 + 操作人
    S->>D: BEGIN TRAN + XACT_ABORT
    S->>D: 按商品锁定库存 UPDLOCK/HOLDLOCK
    alt 任一库存不足
        S->>D: THROW 51001 + ROLLBACK
        A-->>C: 409 inventory.insufficient
    else 全部充足
        S->>D: 写订单和明细
        S->>D: 原子扣减库存并写库存流水
        S->>D: COMMIT
        A-->>C: 200 + 已过账单据
    end
```

应用层库存判断只能改善错误提示，最终一致性由数据库事务和锁保证。出库明细按商品 ID 排序传入，减少不同请求锁顺序不一致造成的死锁概率。

## SQL 对象

- `vw_CurrentInventory`：实时库存、商品和仓库的统一查询口径。
- `vw_InventoryWarnings`：当前库存不高于安全库存的库存位。
- `StockOrderItemType`：多明细单据的表值参数。
- `usp_PostInboundOrder`：原子写入入库单、库存和流水。
- `usp_PostOutboundOrder`：锁定、校验、扣减、写单和流水。
- `usp_GetInventoryReport`：报表统一数据源。

数据库保存 UTC，客户端按 Asia/Shanghai 展示。
