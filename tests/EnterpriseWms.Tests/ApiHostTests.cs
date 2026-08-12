using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using EnterpriseWms.Contracts;
using EnterpriseWms.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseWms.Tests;

public sealed class WarehouseApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = "EnterpriseWmsApiTests_" + Guid.NewGuid().ToString("N");
    private readonly string _connectionString;
    private bool _databaseInitialized;

    public WarehouseApiFactory()
    {
        _connectionString = $"Server=(localdb)\\MSSQLLocalDB;Database={_databaseName};Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
        Environment.SetEnvironmentVariable("Security__JwtKey", "TESTING-ONLY-JWT-KEY-00000000000000000000000000000000");
        Environment.SetEnvironmentVariable("Security__SoapApiKey", "TESTING-ONLY-SOAP-KEY");
        DatabaseInitializer.InitializeAsync(Services).GetAwaiter().GetResult();
        _databaseInitialized = true;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:WarehouseDb"] = _connectionString,
            ["Security:JwtKey"] = "TESTING-ONLY-JWT-KEY-00000000000000000000000000000000",
            ["Security:SoapApiKey"] = "TESTING-ONLY-SOAP-KEY"
        }));
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && _databaseInitialized)
        {
            try
            {
                using var scope = Services.CreateScope();
                scope.ServiceProvider.GetRequiredService<WarehouseDbContext>().Database.EnsureDeleted();
            }
            catch
            {
                // 测试清理不覆盖原始测试结果；数据库名为本次运行独有。
            }
        }
        base.Dispose(disposing);
    }
}

public sealed class ApiHostTests : IClassFixture<WarehouseApiFactory>
{
    private readonly HttpClient _client;
    public ApiHostTests(WarehouseApiFactory factory) => _client = factory.CreateClient();

    private async Task<LoginResponse> LoginAsync(string username, string password)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest { Username = username, Password = password });
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, $"{response.StatusCode}: {body}");
        return (await response.Content.ReadFromJsonAsync<LoginResponse>())!;
    }

    private void UseToken(string token) => _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    [Fact]
    public async Task HealthEndpointIsAvailable()
    {
        var response = await _client.GetAsync("/health");
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"{response.StatusCode}: {body}");
        Assert.Contains("healthy", body);
    }

    [Fact]
    public async Task SwaggerContainsOutboundOrderApi()
    {
        var json = await _client.GetStringAsync("/swagger/v1/swagger.json");
        Assert.Contains("/api/outbound-orders", json);
        Assert.Contains("Bearer", json);
    }

    [Fact]
    public async Task ProtectedEndpointRejectsAnonymousRequest()
    {
        var response = await _client.GetAsync("/api/products");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SoapWsdlPublishesBothInventoryOperations()
    {
        var response = await _client.GetAsync("/soap/StockQueryService.svc?wsdl");
        var wsdl = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, $"{response.StatusCode}: {wsdl}");

        var documents = new List<string> { wsdl };
        foreach (Match match in Regex.Matches(wsdl, "location=\"(?<url>[^\"]+\\?wsdl=wsdl\\d+)\"", RegexOptions.IgnoreCase))
        {
            var location = new Uri(match.Groups["url"].Value);
            documents.Add(await _client.GetStringAsync(location.PathAndQuery));
        }

        var serviceDescription = string.Join(Environment.NewLine, documents);
        Assert.True(serviceDescription.Contains("GetInventory", StringComparison.Ordinal), serviceDescription);
        Assert.Contains("SearchLowStock", serviceDescription);
        Assert.Contains("BasicHttpBinding_IStockQuerySoapService", serviceDescription);
    }

    [Fact]
    public async Task LoginAndCurrentUserReturnSeededAdminIdentity()
    {
        var login = await LoginAsync("admin", "Admin123!");
        Assert.Equal("Admin", login.User.Role);
        Assert.True(login.ExpiresAtUtc > DateTime.UtcNow.AddHours(7));

        UseToken(login.Token);
        var current = await _client.GetFromJsonAsync<CurrentUserDto>("/api/auth/me");
        Assert.NotNull(current);
        Assert.Equal("admin", current.Username);
        Assert.Equal("Admin", current.Role);
    }

    [Fact]
    public async Task ViewerAndOperatorAreBlockedFromAdministrativeWrites()
    {
        var viewer = await LoginAsync("viewer", "Viewer123!");
        UseToken(viewer.Token);
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync("/api/products?page=1&pageSize=2")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.PostAsJsonAsync("/api/outbound-orders", new CreateStockOrderRequest())).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.PostAsJsonAsync("/api/products", new SaveProductRequest())).StatusCode);

        var warehouseOperator = await LoginAsync("operator", "Operator123!");
        UseToken(warehouseOperator.Token);
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.GetAsync("/api/admin/users")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.PostAsJsonAsync("/api/products", new SaveProductRequest())).StatusCode);

        var admin = await LoginAsync("admin", "Admin123!");
        UseToken(admin.Token);
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync("/api/admin/users")).StatusCode);
    }

    [Fact]
    public async Task InventoryExcelContainsTheSameSeededInventoryData()
    {
        var viewer = await LoginAsync("viewer", "Viewer123!");
        UseToken(viewer.Token);
        var response = await _client.GetAsync("/api/reports/inventory.xlsx");
        var bytes = await response.Content.ReadAsByteArrayAsync();
        Assert.True(response.IsSuccessStatusCode, $"{response.StatusCode}: {Encoding.UTF8.GetString(bytes)}");
        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", response.Content.Headers.ContentType?.MediaType);

        using var workbook = new XLWorkbook(new MemoryStream(bytes));
        var sheet = workbook.Worksheet(1);
        Assert.Equal("SKU", sheet.Cell(1, 3).GetString());
        Assert.Contains("ELEC-001", sheet.Column(3).CellsUsed().Select(x => x.GetString()));
        Assert.True(sheet.LastRowUsed()!.RowNumber() >= 11);
    }

    [Fact]
    public async Task SoapInventoryQueryAcceptsValidKeyAndReturnsFaultForInvalidKey()
    {
        async Task<HttpResponseMessage> SendAsync(string apiKey)
        {
            var envelope = $"""
                <s:Envelope xmlns:s="http://schemas.xmlsoap.org/soap/envelope/">
                  <s:Body>
                    <GetInventory xmlns="urn:enterprise-wms:stock-query:v1">
                      <request xmlns:a="urn:enterprise-wms:stock-query:v1:types">
                        <a:ApiKey>{apiKey}</a:ApiKey>
                        <a:WarehouseCode>WH-SH-01</a:WarehouseCode>
                        <a:Sku>ELEC-001</a:Sku>
                      </request>
                    </GetInventory>
                  </s:Body>
                </s:Envelope>
                """;
            using var request = new HttpRequestMessage(HttpMethod.Post, "/soap/StockQueryService.svc")
            {
                Content = new StringContent(envelope, Encoding.UTF8, "text/xml")
            };
            request.Headers.TryAddWithoutValidation("SOAPAction", "\"urn:enterprise-wms:stock-query:v1/IStockQuerySoapService/GetInventory\"");
            return await _client.SendAsync(request);
        }

        var valid = await SendAsync("TESTING-ONLY-SOAP-KEY");
        var validBody = await valid.Content.ReadAsStringAsync();
        Assert.True(valid.IsSuccessStatusCode, $"{valid.StatusCode}: {validBody}");
        Assert.Contains("ELEC-001", validBody);
        Assert.Contains("WH-SH-01", validBody);

        var invalid = await SendAsync("INVALID-SOAP-KEY");
        var fault = await invalid.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.InternalServerError, invalid.StatusCode);
        Assert.Contains("SOAP API Key", fault);
    }

    [Fact]
    public async Task InsufficientInventoryReturnsConflictProblemDetailsWithStableCode()
    {
        var warehouseOperator = await LoginAsync("operator", "Operator123!");
        UseToken(warehouseOperator.Token);
        var inventory = await _client.GetFromJsonAsync<PagedResult<InventoryDto>>("/api/inventory?pageSize=1");
        var item = Assert.Single(inventory!.Items);
        var response = await _client.PostAsJsonAsync("/api/outbound-orders", new CreateStockOrderRequest
        {
            WarehouseId = item.WarehouseId,
            Items = new List<StockOrderItemRequest>
            {
                new() { ProductId = item.ProductId, Quantity = item.Quantity + 1 }
            }
        });
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var problem = JsonDocument.Parse(body);
        Assert.Equal("inventory.insufficient", problem.RootElement.GetProperty("code").GetString());
        Assert.Equal("inventory.insufficient", problem.RootElement.GetProperty("title").GetString());
    }
}
