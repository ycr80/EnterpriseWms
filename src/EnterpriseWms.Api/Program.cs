using System.Text;
using CoreWCF;
using CoreWCF.Configuration;
using CoreWCF.Description;
using EnterpriseWms.Api;
using EnterpriseWms.Api.Soap;
using EnterpriseWms.Domain;
using EnterpriseWms.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true).AddEnvironmentVariables();
var jwtKey = builder.Configuration["Security:JwtKey"];
if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Length < 32)
    throw new InvalidOperationException("请先运行 scripts/setup.ps1 生成本机安全配置。");

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "EnterpriseWms API", Version = "v1", Description = "企业仓储管理系统 REST API" });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme { Name = "Authorization", Type = SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "JWT", In = ParameterLocation.Header });
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement { [new OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>() });
});
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = "EnterpriseWms",
        ValidAudience = "EnterpriseWms.WinForms",
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.FromMinutes(1)
    };
});
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole(Roles.Admin));
    options.AddPolicy("CanOperate", policy => policy.RequireRole(Roles.Admin, Roles.Operator));
});
builder.Services.AddSingleton(new JwtOptions(jwtKey));
builder.Services.AddScoped<JwtTokenService>();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddServiceModelServices().AddServiceModelMetadata();
builder.Services.AddScoped<StockQuerySoapService>();

var app = builder.Build();
app.UseMiddleware<ApiExceptionMiddleware>();
app.UseSwagger();
app.UseSwaggerUI();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<OperationLogMiddleware>();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "healthy", utc = DateTime.UtcNow })).AllowAnonymous();

app.UseServiceModel(serviceBuilder =>
{
    if (app.Environment.IsEnvironment("Testing"))
    {
        serviceBuilder.AddService<StockQuerySoapService>(options =>
                options.BaseAddresses.Add(new Uri("http://localhost")))
            .AddServiceEndpoint<StockQuerySoapService, IStockQuerySoapService>(new BasicHttpBinding(), "/soap/StockQueryService.svc");
    }
    else
    {
        serviceBuilder.AddService<StockQuerySoapService>()
            .AddServiceEndpoint<StockQuerySoapService, IStockQuerySoapService>(new BasicHttpBinding(), "/soap/StockQueryService.svc");
    }
});
var metadata = app.Services.GetRequiredService<ServiceMetadataBehavior>();
metadata.HttpGetEnabled = true;

if (!app.Environment.IsEnvironment("Testing"))
    await DatabaseInitializer.InitializeAsync(app.Services);

if (args.Contains("--initialize-only", StringComparer.OrdinalIgnoreCase))
    return;

app.Run();

public partial class Program { }
