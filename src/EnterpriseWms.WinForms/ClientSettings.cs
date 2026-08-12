using System;
using System.IO;
using System.Text.Json;

namespace EnterpriseWms.WinForms;

internal sealed class ClientSettings
{
    public string ApiBaseAddress { get; set; } = "http://localhost:5080/";
    public string SoapApiKey { get; set; } = string.Empty;
    public static ClientSettings Current { get; private set; } = new();
    public static void Load()
    {
        var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "client.local.json");
        if (File.Exists(path)) Current = JsonSerializer.Deserialize<ClientSettings>(File.ReadAllText(path), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new ClientSettings();
    }
}
