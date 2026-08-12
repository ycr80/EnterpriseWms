# REST API 摘要

除登录、健康检查、Swagger 和 WSDL 外，接口均要求 `Authorization: Bearer <token>`。

| 模块 | 方法与路径 | 权限 |
|---|---|---|
| 登录 | `POST /api/auth/login`、`GET /api/auth/me` | 匿名 / 已登录 |
| 商品 | `GET/POST /api/products`、`PUT/PATCH /api/products/{id}` | 查询所有角色；写入管理员 |
| 仓库 | `GET/POST /api/warehouses`、`PUT/PATCH /api/warehouses/{id}` | 查询所有角色；写入管理员 |
| 库存 | `GET /api/inventory`、`PATCH /api/inventory/{id}/safety-stock` | 查询所有角色；阈值管理员 |
| 入库 | `GET/POST /api/inbound-orders`、`GET /api/inbound-orders/{id}` | 写入管理员/操作员 |
| 出库 | `GET/POST /api/outbound-orders`、`GET /api/outbound-orders/{id}` | 写入管理员/操作员 |
| 报表 | `GET /api/reports/dashboard`、`GET /api/reports/inventory.xlsx` | 所有角色 |
| 管理 | `/api/admin/users`、`/api/admin/operation-logs` | 管理员 |

业务失败统一返回 RFC ProblemDetails，并额外提供稳定 `code`，例如：

```json
{
  "title": "inventory.insufficient",
  "status": 409,
  "detail": "库存不足：ELEC-001 工业扫码枪",
  "code": "inventory.insufficient",
  "traceId": "..."
}
```

SOAP 服务发布 `GetInventoryAsync` 和 `SearchLowStockAsync`，请求携带初始化脚本生成的内部 API Key。
