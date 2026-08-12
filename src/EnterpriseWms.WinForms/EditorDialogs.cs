using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using EnterpriseWms.Contracts;

namespace EnterpriseWms.WinForms;

internal abstract class EditorDialog : Form
{
    protected readonly TableLayoutPanel LayoutPanel = new() { Dock = DockStyle.Fill, ColumnCount = 2, AutoScroll = true, Padding = new Padding(22) };
    protected readonly Button SaveButton = UiTheme.Button("保存", true);
    protected EditorDialog(string title, int width = 520, int height = 480)
    {
        Text = title; StartPosition = FormStartPosition.CenterParent; ClientSize = new Size(width, height); BackColor = Color.White;
        LayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110)); LayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); Controls.Add(LayoutPanel);
        AcceptButton = SaveButton;
    }
    protected TextBox AddText(string label, string value = "")
    {
        var box = new TextBox { Text = value, Dock = DockStyle.Top, Margin = new Padding(4, 8, 4, 8) };
        LayoutPanel.Controls.Add(new Label { Text = label, AutoSize = true, Margin = new Padding(4, 11, 4, 8) }); LayoutPanel.Controls.Add(box); return box;
    }
    protected void AddSave() { LayoutPanel.Controls.Add(new Label()); LayoutPanel.Controls.Add(SaveButton); }
    protected async Task RunAsync(Func<Task> action)
    {
        SaveButton.Enabled = false;
        try { await action(); DialogResult = DialogResult.OK; Close(); }
        catch (Exception exception) { MessageBox.Show(this, exception.Message, "保存失败", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        finally { SaveButton.Enabled = true; }
    }
}

internal sealed class ProductDialog : EditorDialog
{
    public ProductDialog(ApiClient client) : base("新增商品")
    {
        var sku = AddText("SKU"); var name = AddText("商品名称"); var category = AddText("分类"); var spec = AddText("规格"); var unit = AddText("单位"); AddSave();
        SaveButton.Click += async (_, __) => await RunAsync(() => client.CreateProductAsync(new SaveProductRequest { Sku = sku.Text, Name = name.Text, Category = category.Text, Specification = spec.Text, Unit = unit.Text }));
    }
}

internal sealed class WarehouseDialog : EditorDialog
{
    public WarehouseDialog(ApiClient client) : base("新增仓库", 520, 360)
    {
        var code = AddText("仓库编码"); var name = AddText("仓库名称"); var address = AddText("地址"); AddSave();
        SaveButton.Click += async (_, __) => await RunAsync(() => client.CreateWarehouseAsync(new SaveWarehouseRequest { Code = code.Text, Name = name.Text, Address = address.Text }));
    }
}

internal sealed class UserDialog : EditorDialog
{
    public UserDialog(ApiClient client) : base("新增用户", 520, 420)
    {
        var username = AddText("用户名"); var displayName = AddText("显示名称"); var password = AddText("初始密码"); password.UseSystemPasswordChar = true;
        LayoutPanel.Controls.Add(new Label { Text = "角色", AutoSize = true, Margin = new Padding(4, 11, 4, 8) });
        var role = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Top, Margin = new Padding(4, 8, 4, 8) };
        role.Items.AddRange(new object[] { "Operator", "Viewer", "Admin" }); role.SelectedIndex = 0; LayoutPanel.Controls.Add(role); AddSave();
        SaveButton.Click += async (_, __) => await RunAsync(() => client.CreateUserAsync(new SaveUserRequest { Username = username.Text, DisplayName = displayName.Text, Password = password.Text, Role = role.Text }));
    }
}

internal sealed class OrderDialog : Form
{
    private readonly ApiClient _client;
    private readonly string _type;
    private readonly ComboBox _warehouse = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 260 };
    private readonly ComboBox _product = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 280 };
    private readonly NumericUpDown _quantity = new() { DecimalPlaces = 3, Minimum = 0.001m, Maximum = 1000000m, Value = 1, Width = 120 };
    private readonly NumericUpDown _unitCost = new() { DecimalPlaces = 2, Minimum = 0, Maximum = 1000000m, Width = 120 };
    private readonly TextBox _remark = new() { Width = 520 };
    private readonly DataGridView _grid = new() { Dock = DockStyle.Fill };
    private readonly List<StockOrderItemRequest> _items = new();
    private List<ProductDto> _products = new();

    public OrderDialog(ApiClient client, string type)
    {
        _client = client; _type = type; Text = type == "inbound" ? "新建入库单" : "新建出库单"; StartPosition = FormStartPosition.CenterParent; ClientSize = new Size(820, 580); BackColor = Color.White;
        UiTheme.StyleGrid(_grid);
        var top = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 125, Padding = new Padding(12), AutoScroll = true };
        top.Controls.AddRange(new Control[] { Label("仓库"), _warehouse, Label("商品"), _product, Label("数量"), _quantity });
        if (type == "inbound") top.Controls.AddRange(new Control[] { Label("单价"), _unitCost });
        var add = UiTheme.Button("添加明细"); add.Click += (_, __) => AddItem(); top.Controls.Add(add); top.Controls.Add(Label("备注")); top.Controls.Add(_remark);
        var bottom = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 55, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(12) };
        var submit = UiTheme.Button("确认并立即过账", true); submit.Click += async (_, __) => await SubmitAsync(submit); var remove = UiTheme.Button("删除选中明细"); remove.Click += (_, __) => RemoveItem();
        bottom.Controls.Add(submit); bottom.Controls.Add(remove); Controls.Add(_grid); Controls.Add(bottom); Controls.Add(top);
        Shown += async (_, __) => await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        try
        {
            var warehouses = (await _client.GetWarehousesAsync()).Items.ToList(); _products = (await _client.GetProductsAsync()).Items.ToList();
            _warehouse.DataSource = warehouses; _warehouse.DisplayMember = "Name"; _warehouse.ValueMember = "Id";
            _product.DataSource = _products; _product.DisplayMember = "Name"; _product.ValueMember = "Id";
        }
        catch (Exception exception) { MessageBox.Show(this, exception.Message, "加载失败"); }
    }

    private void AddItem()
    {
        if (!(_product.SelectedItem is ProductDto product)) return;
        if (_items.Any(x => x.ProductId == product.Id)) { MessageBox.Show(this, "同一商品不能重复添加。"); return; }
        _items.Add(new StockOrderItemRequest { ProductId = product.Id, Quantity = _quantity.Value, UnitCost = _type == "inbound" ? _unitCost.Value : (decimal?)null }); RefreshLines();
    }
    private void RemoveItem() { if (_grid.CurrentRow?.Tag is int id) { _items.RemoveAll(x => x.ProductId == id); RefreshLines(); } }
    private void RefreshLines()
    {
        _grid.Columns.Clear(); _grid.Rows.Clear(); _grid.Columns.Add("Sku", "SKU"); _grid.Columns.Add("Name", "商品名称"); _grid.Columns.Add("Quantity", "数量"); if (_type == "inbound") _grid.Columns.Add("UnitCost", "单价");
        foreach (var item in _items) { var product = _products.Single(x => x.Id == item.ProductId); var index = _grid.Rows.Add(product.Sku, product.Name, item.Quantity, item.UnitCost); _grid.Rows[index].Tag = item.ProductId; }
    }
    private async Task SubmitAsync(Button button)
    {
        if (!(_warehouse.SelectedItem is WarehouseDto warehouse) || _items.Count == 0) { MessageBox.Show(this, "请选择仓库并添加至少一条明细。"); return; }
        button.Enabled = false;
        try { var order = await _client.CreateOrderAsync(_type, new CreateStockOrderRequest { WarehouseId = warehouse.Id, Remark = _remark.Text, Items = _items }); MessageBox.Show(this, $"过账成功：{order.OrderNo}", "完成"); DialogResult = DialogResult.OK; Close(); }
        catch (Exception exception) { MessageBox.Show(this, exception.Message, "过账失败", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        finally { button.Enabled = true; }
    }
    private static Label Label(string text) => new() { Text = text, AutoSize = true, Margin = new Padding(8, 8, 2, 2) };
}
