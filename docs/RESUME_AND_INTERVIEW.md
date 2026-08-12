# 简历描述与面试准备

## 简历项目描述

**企业仓储管理系统｜C# / .NET 10 / WinForms / SQL Server**

- 基于 ASP.NET Core Web API、EF Core、SQL Server 与 .NET Framework 4.8 WinForms 完成企业仓储管理系统，采用分层架构实现桌面客户端、REST/SOAP 服务与数据持久化。
- 完成商品、仓库、多明细入库/出库、实时库存、库存预警和组合检索模块，并实现库存仪表盘及 Excel/Crystal Reports 报表方案。
- 针对并发出库设计表值参数和事务存储过程，通过 `UPDLOCK/HOLDLOCK` 原子完成库存校验、扣减、单据及库存流水写入；库存不足时整单回滚并返回稳定业务错误码。
- 实现 JWT 登录、管理员/操作员/查看员 RBAC、操作日志及 Swagger，并使用 CoreWCF 提供 BasicHttpBinding SOAP/WSDL 库存查询接口以兼容传统系统。
- 使用 xUnit 和真实 LocalDB 建立 18 项自动化验收，覆盖多明细入库、库存不足回滚、并发出库、三角色权限、HTTP 409 错误码、筛选分页、Excel 内容及 SOAP Fault，验证库存与业务单据的一致性。

## 面试讲解顺序

1. 先说明为什么采用“现代 .NET 后端 + net48 WinForms”的混合架构：后端获得 LTS、EF Core 和 Web API 能力，客户端保留岗位要求的 Crystal Reports 兼容性。
2. 展示 ER 图和 WinForms，不要从 CRUD 开始讲。
3. 用两个并发请求各出库 7、初始库存 10 的测试讲一致性设计。
4. 说明 REST 是主业务接口，SOAP 是传统系统兼容边界，而不是重复实现全部业务。
5. 展示角色权限、操作日志、Swagger、Excel 和数据库视图/过程。

## 高频问题

### 为什么不能先查库存再调用 `SaveChanges`？

两个请求可能同时读到相同库存并同时通过应用层判断。最终校验和扣减必须放入同一数据库事务，并对目标库存位加更新锁。

### 事务里为什么还要固定商品顺序？

多明细订单同时锁多个库存位。不同请求锁顺序相反会增加死锁概率；客户端按商品 ID 排序传入，过程按同样顺序读取目标库存。

### 为什么没有允许修改已过账单据？

直接修改会破坏库存流水和审计链。企业系统通常通过反向业务单纠错，保留原始事实。

### EF Core 和存储过程如何分工？

主数据 CRUD 和普通查询使用 EF Core，降低样板代码；需要强事务、锁和批量表值参数的过账逻辑使用存储过程，让数据库成为最终一致性边界。

### REST 和 WebService 的关系？

REST API 是主接口。岗位中的 WebService 往往指传统 SOAP/WSDL，因此项目使用 CoreWCF 额外暴露只读库存查询，并由 WinForms 通过 `BasicHttpBinding` 实际调用。

### Crystal Reports 为什么放在 net48？

SAP Crystal Reports for Visual Studio 基于 .NET Framework/COM，不支持现代 .NET。将报表留在 net48 客户端，可以在不牺牲现代后端的情况下保持兼容。
