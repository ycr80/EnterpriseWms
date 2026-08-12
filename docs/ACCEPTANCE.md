# 端到端验收记录

验收日期：2026-08-11（Asia/Shanghai）

## 环境

- .NET SDK：10.0.302（项目也可由仓库内 10.0.100 SDK 构建）
- SQL Server Express LocalDB：`(localdb)\MSSQLLocalDB`
- 独立验收数据库：`EnterpriseWmsAcceptance_20260811`
- WinForms：.NET Framework 4.8，Release x64
- SAP Crystal Reports：SP40 x64，13.0.40.5789

## 自动化验收

执行 `dotnet test tests\EnterpriseWms.Tests\EnterpriseWms.Tests.csproj -c Release`：

- 18 项通过，0 失败，0 跳过。
- 覆盖多明细入库、库存不足整单回滚、并发出库竞争、组合筛选分页、停用商品限制与历史查询。
- 覆盖登录/JWT、管理员/操作员/查看员权限边界、Swagger、库存不足 HTTP 409 ProblemDetails 与稳定错误码、Excel 内容、WSDL、合法 SOAP Key 和无效 Key Fault。

## 全新数据库端到端流程

通过以下命令从零创建独立数据库：

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\setup.ps1 -DatabaseName EnterpriseWmsAcceptance_20260811
```

初始化结果：EF Migration 成功，视图、表值参数和存储过程创建成功，解决方案构建 0 警告、0 错误，演示数据写入成功。

随后通过真实 HTTP API 执行：

| 检查项 | 结果 |
|---|---|
| 管理员登录 | `Admin`，成功签发 JWT |
| 初始库存 | 50.000 |
| 入库 | +5，生成 `IN` 单号 |
| 出库 | -2，生成 `OUT` 单号 |
| 最终库存 | 53.000，与预期一致 |
| 库存报表数据 | 10 行 |
| 库存预警 | 1 项 |
| Excel 下载 | 8,438 字节，可由 ClosedXML 解析 |
| SOAP WSDL | HTTP 200 |
| 健康检查 | `healthy` |

## Crystal Reports 验收

管理员从 WinForms 的“库存报表”打开真实 `.rpt` 模板，报表从 REST API 返回的 `DataTable` 获取 10 行数据，不保存数据库账号。中文标题、中文列头、记录数和生成时间均正常显示，打印/导出工具栏可用，并已通过界面成功导出 [CrystalInventoryReport-Acceptance.pdf](CrystalInventoryReport-Acceptance.pdf)。
