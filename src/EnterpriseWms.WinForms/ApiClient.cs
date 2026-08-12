using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using EnterpriseWms.Contracts;

namespace EnterpriseWms.WinForms;

internal sealed class ApiClient : IDisposable
{
    private readonly HttpClient _http;
    private readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    public CurrentUserDto? CurrentUser { get; private set; }
    public Uri BaseAddress => _http.BaseAddress!;

    public ApiClient(string baseAddress) => _http = new HttpClient { BaseAddress = new Uri(baseAddress), Timeout = TimeSpan.FromSeconds(30) };

    public async Task LoginAsync(string username, string password)
    {
        var response = await SendAsync<LoginResponse>(HttpMethod.Post, "api/auth/login", new LoginRequest { Username = username, Password = password });
        CurrentUser = response.User;
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", response.Token);
    }

    public Task<DashboardDto> GetDashboardAsync() => GetAsync<DashboardDto>("api/reports/dashboard");
    public Task<PagedResult<ProductDto>> GetProductsAsync(string keyword = "") => GetAsync<PagedResult<ProductDto>>($"api/products?active=true&pageSize=100&keyword={Uri.EscapeDataString(keyword)}");
    public Task<PagedResult<WarehouseDto>> GetWarehousesAsync(string keyword = "") => GetAsync<PagedResult<WarehouseDto>>($"api/warehouses?active=true&pageSize=100&keyword={Uri.EscapeDataString(keyword)}");
    public Task<PagedResult<InventoryDto>> GetInventoryAsync(string keyword = "", bool warningOnly = false) => GetAsync<PagedResult<InventoryDto>>($"api/inventory?pageSize=200&warningOnly={warningOnly.ToString().ToLowerInvariant()}&keyword={Uri.EscapeDataString(keyword)}");
    public Task<PagedResult<StockOrderDto>> GetOrdersAsync(string type) => GetAsync<PagedResult<StockOrderDto>>($"api/{type}-orders?pageSize=100");
    public Task<IReadOnlyList<UserDto>> GetUsersAsync() => GetAsync<IReadOnlyList<UserDto>>("api/admin/users");
    public Task<PagedResult<OperationLogDto>> GetLogsAsync() => GetAsync<PagedResult<OperationLogDto>>("api/admin/operation-logs?pageSize=100");
    public Task<ProductDto> CreateProductAsync(SaveProductRequest request) => SendAsync<ProductDto>(HttpMethod.Post, "api/products", request);
    public Task<WarehouseDto> CreateWarehouseAsync(SaveWarehouseRequest request) => SendAsync<WarehouseDto>(HttpMethod.Post, "api/warehouses", request);
    public Task<StockOrderDto> CreateOrderAsync(string type, CreateStockOrderRequest request) => SendAsync<StockOrderDto>(HttpMethod.Post, $"api/{type}-orders", request);
    public Task<UserDto> CreateUserAsync(SaveUserRequest request) => SendAsync<UserDto>(HttpMethod.Post, "api/admin/users", request);

    public async Task DownloadInventoryExcelAsync(string fileName, bool warningOnly)
    {
        using var response = await _http.GetAsync($"api/reports/inventory.xlsx?warningOnly={warningOnly.ToString().ToLowerInvariant()}");
        await EnsureSuccessAsync(response);
        using var source = await response.Content.ReadAsStreamAsync();
        using var target = File.Create(fileName);
        await source.CopyToAsync(target);
    }

    public async Task<T> GetAsync<T>(string path)
    {
        using var response = await _http.GetAsync(path);
        await EnsureSuccessAsync(response);
        var stream = await response.Content.ReadAsStreamAsync();
        return (await JsonSerializer.DeserializeAsync<T>(stream, _json))!;
    }

    private async Task<T> SendAsync<T>(HttpMethod method, string path, object body)
    {
        var json = JsonSerializer.Serialize(body, _json);
        using var request = new HttpRequestMessage(method, path) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
        using var response = await _http.SendAsync(request);
        await EnsureSuccessAsync(response);
        var stream = await response.Content.ReadAsStreamAsync();
        return (await JsonSerializer.DeserializeAsync<T>(stream, _json))!;
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode) return;
        var content = await response.Content.ReadAsStringAsync();
        try
        {
            using var document = JsonDocument.Parse(content);
            var root = document.RootElement;
            var message = root.TryGetProperty("detail", out var detail) ? detail.GetString() : root.TryGetProperty("message", out var value) ? value.GetString() : content;
            throw new ApiException((int)response.StatusCode, message ?? "请求失败。");
        }
        catch (JsonException) { throw new ApiException((int)response.StatusCode, string.IsNullOrWhiteSpace(content) ? response.ReasonPhrase ?? "请求失败。" : content); }
    }

    public void Dispose() => _http.Dispose();
}

internal sealed class ApiException : Exception
{
    public ApiException(int statusCode, string message) : base(message) => StatusCode = statusCode;
    public int StatusCode { get; }
}
