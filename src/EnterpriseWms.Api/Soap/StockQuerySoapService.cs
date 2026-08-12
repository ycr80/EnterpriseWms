using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using CoreWCF;
using EnterpriseWms.Application;

namespace EnterpriseWms.Api.Soap;

[ServiceContract(Namespace = "urn:enterprise-wms:stock-query:v1")]
public interface IStockQuerySoapService
{
    [OperationContract]
    Task<InventorySoapItem?> GetInventory(StockQueryRequest request);

    [OperationContract]
    Task<InventorySoapItem[]> SearchLowStock(LowStockQueryRequest request);
}

[DataContract(Namespace = "urn:enterprise-wms:stock-query:v1:types")]
public sealed class StockQueryRequest
{
    [DataMember(Order = 1)] public string ApiKey { get; set; } = string.Empty;
    [DataMember(Order = 2)] public string WarehouseCode { get; set; } = string.Empty;
    [DataMember(Order = 3)] public string Sku { get; set; } = string.Empty;
}

[DataContract(Namespace = "urn:enterprise-wms:stock-query:v1:types")]
public sealed class LowStockQueryRequest
{
    [DataMember(Order = 1)] public string ApiKey { get; set; } = string.Empty;
    [DataMember(Order = 2)] public string WarehouseCode { get; set; } = string.Empty;
}

[DataContract(Namespace = "urn:enterprise-wms:stock-query:v1:types")]
public sealed class InventorySoapItem
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

public sealed class StockQuerySoapService : IStockQuerySoapService
{
    private readonly IInventoryService _inventory;
    private readonly byte[] _expectedKey;
    public StockQuerySoapService(IInventoryService inventory, IConfiguration configuration)
    {
        _inventory = inventory;
        var configuredKey = configuration["Security:SoapApiKey"];
        if (string.IsNullOrWhiteSpace(configuredKey)) throw new InvalidOperationException("缺少 Security:SoapApiKey 配置。");
        _expectedKey = Encoding.UTF8.GetBytes(configuredKey);
    }

    public async Task<InventorySoapItem?> GetInventory(StockQueryRequest request)
    {
        ValidateKey(request.ApiKey);
        var item = await _inventory.GetByCodesAsync(request.WarehouseCode, request.Sku, CancellationToken.None);
        return item == null ? null : Map(item);
    }

    public async Task<InventorySoapItem[]> SearchLowStock(LowStockQueryRequest request)
    {
        ValidateKey(request.ApiKey);
        var items = await _inventory.GetWarningsByWarehouseCodeAsync(request.WarehouseCode, CancellationToken.None);
        return items.Select(Map).ToArray();
    }

    private void ValidateKey(string supplied)
    {
        var actual = Encoding.UTF8.GetBytes(supplied ?? string.Empty);
        if (actual.Length != _expectedKey.Length || !CryptographicOperations.FixedTimeEquals(actual, _expectedKey))
            throw new FaultException("SOAP API Key 无效。");
    }

    private static InventorySoapItem Map(EnterpriseWms.Contracts.InventoryDto x) => new()
    {
        WarehouseCode = x.WarehouseCode, WarehouseName = x.WarehouseName, Sku = x.Sku, ProductName = x.ProductName,
        Unit = x.Unit, Quantity = x.Quantity, SafetyStock = x.SafetyStock, IsLowStock = x.IsLowStock, UpdatedAtUtc = x.UpdatedAtUtc
    };
}
