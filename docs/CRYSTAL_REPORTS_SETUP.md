# Crystal Reports 接入与验收

SAP Crystal Reports for Visual Studio 依赖 .NET Framework 和 COM，不能由 NuGet 还原，也不能随本项目重新分发。官方说明现代 .NET 不受支持，因此报表留在 net48 x64 WinForms 客户端。

## 安装

1. 在 Visual Studio Installer 中安装“.NET 桌面开发”和“.NET Framework 4.8 Targeting Pack”。
2. 从 [SAP 官方注册页](https://www.sap.com/registration/trial.9a4afb3b-7eaa-42af-98ce-abeae5deb784.html) 下载 Crystal Reports for Visual Studio SP40 x64。
3. 右键以管理员身份安装开发组件，并在安装末尾安装 x64 Runtime。
4. 在 VS2022 中确认工具箱出现 `CrystalReportViewer`。

## 报表设计

项目已包含可运行的 `Reports/InventoryReport.rpt` 和 `Reports/InventoryReportDataSet.xsd`。模板通过 ADO.NET XML 架构定义字段，不保存 SQL Server 账号；`CrystalInventoryPreviewForm` 先从 REST API 获取 `InventoryDto`，转换为同结构 `DataTable`，再调用 `ReportDocument.SetDataSource(dataTable)`。

报表当前展示仓库名称、商品名称、SKU、当前库存和安全库存，预览窗体增加中文标题、记录数、生成时间和独立“导出 PDF”按钮；`CrystalReportViewer` 同时保留刷新、打印、导出和分页工具栏。项目文件引用本机 GAC 中的 SAP 13.0.4000.0 程序集，并把 `.rpt` 复制到 x64 WinForms 输出目录。

## 验收

- WinForms 编译为 x64，SAP runtime 也必须为 x64。
- 报表数据必须来自 API，不直接连接数据库。
- Crystal 预览行数与 Excel、`vw_CurrentInventory` 在相同筛选条件下保持一致。
- 预览、打印和导出 PDF 各执行一次。

## 当前验收结果

- SAP Crystal Reports for Visual Studio SP40 x64：`13.0.40.5789`。
- .NET Framework 4.8 WinForms Debug/Release x64：0 警告、0 错误。
- 管理员登录后进入“库存报表”，Crystal 预览成功渲染 10 条演示库存数据。
- 已通过真实保存对话框成功导出 `docs/CrystalInventoryReport-Acceptance.pdf`（15,588 字节）。
- 验收截图：`docs/screenshots/crystal-inventory-report.jpg`。
