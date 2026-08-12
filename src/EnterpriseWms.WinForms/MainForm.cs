using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using EnterpriseWms.Contracts;

namespace EnterpriseWms.WinForms;

internal sealed class MainForm : Form
{
    private readonly ApiClient _client;
    private readonly Panel _content = new() { Dock = DockStyle.Fill, BackColor = UiTheme.Background, Padding = new Padding(22) };
    private readonly Label _title = new() { Dock = DockStyle.Top, Height = 48, Font = new Font("Microsoft YaHei UI", 18F, FontStyle.Bold), ForeColor = Color.FromArgb(15, 23, 42) };
    private readonly FlowLayoutPanel _toolbar = new() { Dock = DockStyle.Top, Height = 48, FlowDirection = FlowDirection.LeftToRight };
    private readonly TextBox _filter = new() { Width = 260, Height = 34, Margin = new Padding(0, 5, 8, 5) };
    private readonly Button _refresh = UiTheme.Button("查询/刷新", true);
    private readonly Button _primary = UiTheme.Button("新增", true);
    private readonly Button _export = UiTheme.Button("导出 Excel");
    private readonly Button _crystal = UiTheme.Button("Crystal 预览");
    private readonly DataGridView _grid = new() { Dock = DockStyle.Fill };
    private readonly Label _status = new() { Dock = DockStyle.Bottom, Height = 28, ForeColor = Color.Gray };
    private string _module = "dashboard";
    private bool _warningOnly;

    public MainForm(ApiClient client)
    {
        _client = client;
        Text = $"EnterpriseWms - {client.CurrentUser!.DisplayName}";
        WindowState = FormWindowState.Maximized;
        MinimumSize = new Size(1100, 700);
        Font = new Font("Microsoft YaHei UI", 9F);
        UiTheme.StyleGrid(_grid);

        var sidebar = BuildSidebar();
        Controls.Add(_content);
        Controls.Add(sidebar);
        _content.Controls.Add(_grid);
        _content.Controls.Add(_status);
        _content.Controls.Add(_toolbar);
        _content.Controls.Add(_title);
        _toolbar.Controls.Add(_filter);
        _toolbar.Controls.Add(_refresh);
        _toolbar.Controls.Add(_primary);
        _toolbar.Controls.Add(_crystal);
        _toolbar.Controls.Add(_export);
        _filter.KeyDown += async (_, e) => { if (e.KeyCode == Keys.Enter) await RefreshAsync(); };
        _refresh.Click += async (_, __) => await RefreshAsync();
        _primary.Click += async (_, __) => await PrimaryActionAsync();
        _crystal.Click += async (_, __) => await PreviewCrystalAsync();
        _export.Click += async (_, __) => await ExportAsync();
        Shown += async (_, __) => await ShowModuleAsync("dashboard", "库存仪表盘");
    }

    private Panel BuildSidebar()
    {
        var panel = new Panel { Dock = DockStyle.Left, Width = 220, BackColor = UiTheme.Sidebar };
        panel.Controls.Add(new Label { Text = "EnterpriseWms", Dock = DockStyle.Top, Height = 70, ForeColor = Color.White, Font = new Font("Microsoft YaHei UI", 15F, FontStyle.Bold), TextAlign = ContentAlignment.MiddleCenter });
        var nav = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(10) };
        panel.Controls.Add(nav);
        AddNav(nav, "仪表盘", "dashboard");
        AddNav(nav, "商品管理", "products");
        AddNav(nav, "仓库管理", "warehouses");
        if (_client.CurrentUser!.Role != "Viewer")
        {
            AddNav(nav, "入库单", "inbound");
            AddNav(nav, "出库单", "outbound");
        }
        AddNav(nav, "实时库存", "inventory");
        AddNav(nav, "库存预警", "warnings");
        AddNav(nav, "库存报表", "reports");
        if (_client.CurrentUser.Role != "Viewer")
            AddNav(nav, "传统 WebService", "soap");
        if (_client.CurrentUser!.Role == "Admin")
        {
            AddNav(nav, "用户管理", "users");
            AddNav(nav, "操作日志", "logs");
        }
        panel.Controls.Add(new Label { Text = $"{_client.CurrentUser.DisplayName}\r\n{RoleText(_client.CurrentUser.Role)}", Dock = DockStyle.Bottom, Height = 64, ForeColor = Color.FromArgb(203, 213, 225), TextAlign = ContentAlignment.MiddleCenter });
        return panel;
    }

    private void AddNav(FlowLayoutPanel panel, string text, string module)
    {
        var button = new Button { Text = text, Width = 190, Height = 42, FlatStyle = FlatStyle.Flat, BackColor = UiTheme.Sidebar, ForeColor = Color.White, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(18, 0, 0, 0), Margin = new Padding(0, 2, 0, 2) };
        button.FlatAppearance.BorderSize = 0;
        button.Click += async (_, __) => await ShowModuleAsync(module, text);
        panel.Controls.Add(button);
    }

    private async Task ShowModuleAsync(string module, string title)
    {
        _module = module;
        _title.Text = title;
        _warningOnly = module == "warnings";
        _filter.Visible = module is "products" or "warehouses" or "inventory" or "warnings";
        _primary.Visible =
            (_client.CurrentUser!.Role == "Admin" && module is ("products" or "warehouses" or "users")) ||
            (_client.CurrentUser.Role != "Viewer" && module is ("inbound" or "outbound"));
        _primary.Text = module is "inbound" or "outbound" ? "新建并过账" : "新增";
        _crystal.Visible = module == "reports";
        _export.Visible = module is "inventory" or "warnings" or "reports";
        _grid.Visible = module != "dashboard" && module != "soap";
        RemoveDynamicPanels();
        if (module == "soap")
        {
            var soap = new SoapQueryPanel(_client.BaseAddress) { Dock = DockStyle.Fill };
            _content.Controls.Add(soap);
            soap.BringToFront();
        }
        await RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        SetBusy(true);
        try
        {
            switch (_module)
            {
                case "dashboard": ShowDashboard(await _client.GetDashboardAsync()); break;
                case "products": Bind((await _client.GetProductsAsync(_filter.Text)).Items); break;
                case "warehouses": Bind((await _client.GetWarehousesAsync(_filter.Text)).Items); break;
                case "inventory": case "warnings": case "reports": Bind((await _client.GetInventoryAsync(_filter.Text, _warningOnly)).Items); break;
                case "inbound": Bind((await _client.GetOrdersAsync("inbound")).Items); break;
                case "outbound": Bind((await _client.GetOrdersAsync("outbound")).Items); break;
                case "users": Bind(await _client.GetUsersAsync()); break;
                case "logs": Bind((await _client.GetLogsAsync()).Items); break;
            }
            _status.Text = $"最后刷新：{DateTime.Now:yyyy-MM-dd HH:mm:ss}";
        }
        catch (Exception exception) { MessageBox.Show(this, exception.Message, "操作失败", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        finally { SetBusy(false); }
    }

    private void Bind<T>(IEnumerable<T> items)
    {
        _grid.DataSource = items.ToList();
        var headers = new Dictionary<string, string>
        {
            ["Id"]="ID", ["Sku"]="SKU", ["Name"]="名称", ["Category"]="分类", ["Specification"]="规格", ["Unit"]="单位", ["IsActive"]="启用",
            ["Code"]="编码", ["Address"]="地址", ["WarehouseCode"]="仓库编码", ["WarehouseName"]="仓库", ["ProductName"]="商品名称", ["Quantity"]="当前库存",
            ["SafetyStock"]="安全库存", ["IsLowStock"]="预警", ["UpdatedAtUtc"]="更新时间(UTC)", ["OrderNo"]="单号", ["Type"]="类型", ["OperatorName"]="操作人",
            ["Remark"]="备注", ["PostedAtUtc"]="过账时间(UTC)", ["Username"]="用户名", ["DisplayName"]="显示名称", ["Role"]="角色", ["LastLoginAtUtc"]="最后登录(UTC)",
            ["Module"]="模块", ["Action"]="动作", ["Target"]="目标", ["Result"]="结果", ["ElapsedMilliseconds"]="耗时(ms)", ["IpAddress"]="IP", ["CreatedAtUtc"]="时间(UTC)"
        };
        foreach (DataGridViewColumn column in _grid.Columns)
        {
            if (headers.TryGetValue(column.Name, out var header)) column.HeaderText = header;
            if (column.Name is "Items" or "ProductId" or "WarehouseId") column.Visible = false;
        }
    }

    private void ShowDashboard(DashboardDto data)
    {
        RemoveDynamicPanels();
        var panel = new TableLayoutPanel { Name = "DynamicPanel", Dock = DockStyle.Fill, ColumnCount = 5, RowCount = 2, BackColor = UiTheme.Background, Padding = new Padding(0, 8, 0, 0) };
        for (var i = 0; i < 5; i++) panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 120));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.Controls.Add(Card("启用商品", data.ActiveProductCount.ToString()), 0, 0);
        panel.Controls.Add(Card("启用仓库", data.ActiveWarehouseCount.ToString()), 1, 0);
        panel.Controls.Add(Card("库存预警", data.LowStockCount.ToString(), data.LowStockCount > 0), 2, 0);
        panel.Controls.Add(Card("今日入库单", data.TodayInboundCount.ToString()), 3, 0);
        panel.Controls.Add(Card("今日出库单", data.TodayOutboundCount.ToString()), 4, 0);
        var trend = Chart("近七日订单趋势", SeriesChartType.Column);
        trend.Series.Add(new Series("入库") { ChartType = SeriesChartType.Column, Color = UiTheme.Success });
        trend.Series.Add(new Series("出库") { ChartType = SeriesChartType.Column, Color = UiTheme.Primary });
        foreach (var point in data.OrderTrend) { trend.Series["入库"].Points.AddXY(point.Label, point.Value); trend.Series["出库"].Points.AddXY(point.Label, point.SecondaryValue); }
        panel.Controls.Add(trend, 0, 1);
        panel.SetColumnSpan(trend, 3);
        var warnings = Chart("各仓库预警分布", SeriesChartType.Bar);
        warnings.Series.Add(new Series("预警库存位") { ChartType = SeriesChartType.Bar, Color = UiTheme.Danger });
        foreach (var point in data.WarningByWarehouse) warnings.Series[0].Points.AddXY(point.Label, point.Value);
        panel.Controls.Add(warnings, 3, 1);
        panel.SetColumnSpan(warnings, 2);
        _content.Controls.Add(panel);
        panel.BringToFront();
    }

    private static Control Card(string label, string value, bool danger = false)
    {
        var panel = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Margin = new Padding(6), Padding = new Padding(16) };
        panel.Controls.Add(new Label { Text = value, Dock = DockStyle.Fill, Font = new Font("Microsoft YaHei UI", 24F, FontStyle.Bold), ForeColor = danger ? UiTheme.Danger : UiTheme.Primary, TextAlign = ContentAlignment.MiddleLeft });
        panel.Controls.Add(new Label { Text = label, Dock = DockStyle.Top, Height = 25, ForeColor = Color.Gray });
        return panel;
    }

    private static Chart Chart(string title, SeriesChartType type)
    {
        var chart = new Chart { Dock = DockStyle.Fill, BackColor = Color.White, Margin = new Padding(6) };
        chart.ChartAreas.Add(new ChartArea());
        chart.Titles.Add(title);
        chart.Legends.Add(new Legend());
        return chart;
    }

    private async Task PrimaryActionAsync()
    {
        Form? dialog = null;
        if (_module == "products") dialog = new ProductDialog(_client);
        else if (_module == "warehouses") dialog = new WarehouseDialog(_client);
        else if (_module is "inbound" or "outbound") dialog = new OrderDialog(_client, _module);
        else if (_module == "users") dialog = new UserDialog(_client);
        if (dialog != null) { using (dialog) if (dialog.ShowDialog(this) == DialogResult.OK) await RefreshAsync(); }
    }

    private async Task ExportAsync()
    {
        using var dialog = new SaveFileDialog { Filter = "Excel 工作簿 (*.xlsx)|*.xlsx", FileName = $"库存报表_{DateTime.Now:yyyyMMddHHmm}.xlsx" };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        try { await _client.DownloadInventoryExcelAsync(dialog.FileName, _warningOnly); MessageBox.Show(this, "Excel 报表已导出。", "完成"); }
        catch (Exception exception) { MessageBox.Show(this, exception.Message, "导出失败", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private async Task PreviewCrystalAsync()
    {
        try
        {
            SetBusy(true);
            var rows = (await _client.GetInventoryAsync(_filter.Text, _warningOnly)).Items;
            using var preview = new CrystalInventoryPreviewForm(rows);
            preview.ShowDialog(this);
        }
        catch (Exception exception)
        {
            MessageBox.Show(this, exception.Message, "Crystal Reports 预览失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void SetBusy(bool busy) { _refresh.Enabled = !busy; _primary.Enabled = !busy; _crystal.Enabled = !busy; _export.Enabled = !busy; Cursor = busy ? Cursors.WaitCursor : Cursors.Default; }
    private void RemoveDynamicPanels() { foreach (Control control in _content.Controls.Cast<Control>().Where(x => x.Name == "DynamicPanel").ToList()) { _content.Controls.Remove(control); control.Dispose(); } }
    private static string RoleText(string role) => role == "Admin" ? "管理员" : role == "Operator" ? "仓库操作员" : "只读查看员";
}
