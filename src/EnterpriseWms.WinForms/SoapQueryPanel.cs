using System;
using System.Drawing;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EnterpriseWms.WinForms;

internal sealed class SoapQueryPanel : Panel
{
    private readonly Uri _endpoint;
    private readonly TextBox _apiKey = new() { Width = 220 };
    private readonly TextBox _warehouse = new() { Text = "WH-SH-01", Width = 140 };
    private readonly TextBox _sku = new() { Text = "ELEC-001", Width = 140 };
    private readonly DataGridView _grid = new() { Dock = DockStyle.Fill };

    public SoapQueryPanel(Uri apiBase)
    {
        Name = "DynamicPanel"; _endpoint = new Uri(apiBase, "soap/StockQueryService.svc"); BackColor = UiTheme.Background; _apiKey.Text = ClientSettings.Current.SoapApiKey;
        UiTheme.StyleGrid(_grid);
        var info = new Label { Text = "此页面通过 BasicHttpBinding 调用 SOAP/WSDL 服务，不经过 REST API。", Dock = DockStyle.Top, Height = 34, ForeColor = Color.Gray };
        var toolbar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 55, BackColor = Color.White, Padding = new Padding(8) };
        toolbar.Controls.AddRange(new Control[] { Label("API Key"), _apiKey, Label("仓库编码"), _warehouse, Label("SKU"), _sku });
        var query = UiTheme.Button("查询单品", true); query.Click += async (_, __) => await QueryAsync(false); var warnings = UiTheme.Button("查询低库存"); warnings.Click += async (_, __) => await QueryAsync(true);
        toolbar.Controls.Add(query); toolbar.Controls.Add(warnings); Controls.Add(_grid); Controls.Add(toolbar); Controls.Add(info);
    }

    private async Task QueryAsync(bool warnings)
    {
        var factory = new ChannelFactory<IStockQuerySoapService>(new BasicHttpBinding(), new EndpointAddress(_endpoint));
        var channel = factory.CreateChannel();
        try
        {
            if (warnings)
                _grid.DataSource = await channel.SearchLowStock(new LowStockQueryRequest { ApiKey = _apiKey.Text, WarehouseCode = _warehouse.Text });
            else
            {
                var item = await channel.GetInventory(new StockQueryRequest { ApiKey = _apiKey.Text, WarehouseCode = _warehouse.Text, Sku = _sku.Text });
                _grid.DataSource = item == null ? Array.Empty<InventorySoapItem>() : new[] { item };
            }
        }
        catch (Exception exception) { MessageBox.Show(this, exception.Message, "SOAP 调用失败", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        finally
        {
            try { ((IClientChannel)channel).Close(); factory.Close(); } catch { ((IClientChannel)channel).Abort(); factory.Abort(); }
        }
    }
    private static Label Label(string text) => new() { Text = text, AutoSize = true, Margin = new Padding(8, 8, 2, 2) };
}

[ServiceContract(Name = "IStockQuerySoapService", Namespace = "urn:enterprise-wms:stock-query:v1")]
internal interface IStockQuerySoapService
{
    [OperationContract] Task<InventorySoapItem?> GetInventory(StockQueryRequest request);
    [OperationContract] Task<InventorySoapItem[]> SearchLowStock(LowStockQueryRequest request);
}

[DataContract(Namespace = "urn:enterprise-wms:stock-query:v1:types")]
internal sealed class StockQueryRequest
{
    [DataMember(Order = 1)] public string ApiKey { get; set; } = string.Empty;
    [DataMember(Order = 2)] public string WarehouseCode { get; set; } = string.Empty;
    [DataMember(Order = 3)] public string Sku { get; set; } = string.Empty;
}

[DataContract(Namespace = "urn:enterprise-wms:stock-query:v1:types")]
internal sealed class LowStockQueryRequest
{
    [DataMember(Order = 1)] public string ApiKey { get; set; } = string.Empty;
    [DataMember(Order = 2)] public string WarehouseCode { get; set; } = string.Empty;
}

[DataContract(Namespace = "urn:enterprise-wms:stock-query:v1:types")]
internal sealed class InventorySoapItem
{
    [DataMember(Order = 1)] public string WarehouseCode { get; set; } = string.Empty;
    [DataMember(Order = 2)] public string WarehouseName { get; set; } = string.Empty;
    [DataMember(Order = 3)] public string Sku { get; set; } = string.Empty;
    [DataMember(Order = 4)] public string ProductName { get; set; } = string.Empty;
    [DataMember(Order = 5)] public string Unit { get; set; } = string.Empty;
    [DataMember(Order = 6)] public decimal Quantity { get; set; }
    [DataMember(Order = 7)] public decimal SafetyStock { get; set; }
    [DataMember(Order = 8)] public bool IsLowStock { get; set; }
    [DataMember(Order = 9)] public DateTime UpdatedAtUtc { get; set; }
}
