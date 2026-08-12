using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using CrystalDecisions.CrystalReports.Engine;
using CrystalDecisions.Shared;
using CrystalDecisions.Windows.Forms;
using EnterpriseWms.Contracts;

namespace EnterpriseWms.WinForms;

internal sealed class CrystalInventoryPreviewForm : Form
{
    private readonly ReportDocument _report = new();

    public CrystalInventoryPreviewForm(IEnumerable<InventoryDto> inventoryRows)
    {
        var rows = inventoryRows.ToList();
        Text = "Crystal Reports 库存报表";
        WindowState = FormWindowState.Maximized;
        MinimumSize = new Size(1000, 700);
        Font = new Font("Microsoft YaHei UI", 9F);

        var viewer = new CrystalReportViewer
        {
            Dock = DockStyle.Fill,
            ToolPanelView = ToolPanelViewType.None,
            ShowGroupTreeButton = false,
            ShowParameterPanelButton = false
        };

        var header = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 78,
            BackColor = Color.White,
            Padding = new Padding(20, 10, 20, 8),
            ColumnCount = 2,
            RowCount = 2
        };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        header.RowStyles.Add(new RowStyle(SizeType.Percent, 62F));
        header.RowStyles.Add(new RowStyle(SizeType.Percent, 38F));
        var title = new Label
        {
            Text = "企业仓储管理系统 · 库存明细报表",
            Dock = DockStyle.Fill,
            Font = new Font("Microsoft YaHei UI", 16F, FontStyle.Bold),
            ForeColor = Color.FromArgb(15, 23, 42),
            TextAlign = ContentAlignment.MiddleLeft
        };
        var description = new Label
        {
            Text = $"数据来源：REST API　记录数：{rows.Count}　生成时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}",
            Dock = DockStyle.Fill,
            ForeColor = Color.FromArgb(100, 116, 139),
            TextAlign = ContentAlignment.MiddleLeft
        };
        var exportPdf = UiTheme.Button("导出 PDF", true);
        exportPdf.Width = 112;
        exportPdf.Height = 36;
        exportPdf.Margin = new Padding(12, 7, 0, 0);
        exportPdf.Click += (_, __) => ExportPdf();
        header.Controls.Add(title, 0, 0);
        header.Controls.Add(description, 0, 1);
        header.Controls.Add(exportPdf, 1, 0);
        header.SetRowSpan(exportPdf, 2);

        Controls.Add(viewer);
        Controls.Add(header);

        var reportPath = Path.Combine(Application.StartupPath, "Reports", "InventoryReport.rpt");
        if (!File.Exists(reportPath))
            throw new FileNotFoundException("未找到 Crystal Reports 库存报表文件。", reportPath);

        _report.Load(reportPath);
        LocalizeReportText();
        _report.SetDataSource(CreateInventoryTable(rows));
        viewer.ReportSource = _report;
        viewer.RefreshReport();
        FormClosed += (_, __) =>
        {
            viewer.ReportSource = null;
            _report.Close();
            _report.Dispose();
        };
    }

    private void LocalizeReportText()
    {
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["WarehouseName"] = "仓库",
            ["ProductName"] = "商品名称",
            ["Sku"] = "SKU",
            ["Quantity"] = "当前库存",
            ["SafetyStock"] = "安全库存"
        };

        foreach (Section section in _report.ReportDefinition.Sections)
        foreach (ReportObject reportObject in section.ReportObjects)
        {
            if (reportObject is not TextObject textObject) continue;
            var text = (textObject.Text ?? string.Empty).Trim();
            if (headers.TryGetValue(text, out var localized)) textObject.Text = localized;
            else if (text.Contains("明细报表") || text.Contains("Inventory Report"))
                textObject.Text = "库存明细报表";
        }
    }

    private void ExportPdf()
    {
        using var dialog = new SaveFileDialog
        {
            Filter = "PDF 文件 (*.pdf)|*.pdf",
            FileName = $"库存明细报表_{DateTime.Now:yyyyMMddHHmmss}.pdf",
            AddExtension = true,
            DefaultExt = "pdf"
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            _report.ExportToDisk(ExportFormatType.PortableDocFormat, dialog.FileName);
            MessageBox.Show(this, "Crystal Reports PDF 已导出。", "完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception exception)
        {
            MessageBox.Show(this, exception.Message, "PDF 导出失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static DataTable CreateInventoryTable(IEnumerable<InventoryDto> rows)
    {
        var table = new DataTable("InventoryReport");
        table.Columns.Add("WarehouseCode", typeof(string));
        table.Columns.Add("WarehouseName", typeof(string));
        table.Columns.Add("Sku", typeof(string));
        table.Columns.Add("ProductName", typeof(string));
        table.Columns.Add("Category", typeof(string));
        table.Columns.Add("Specification", typeof(string));
        table.Columns.Add("Unit", typeof(string));
        table.Columns.Add("Quantity", typeof(decimal));
        table.Columns.Add("SafetyStock", typeof(decimal));
        table.Columns.Add("IsLowStock", typeof(bool));
        table.Columns.Add("UpdatedAtUtc", typeof(DateTime));

        foreach (var item in rows)
        {
            table.Rows.Add(item.WarehouseCode, item.WarehouseName, item.Sku, item.ProductName,
                item.Category, item.Specification, item.Unit, item.Quantity, item.SafetyStock,
                item.IsLowStock, item.UpdatedAtUtc);
        }

        return table;
    }
}
