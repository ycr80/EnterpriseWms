using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using EnterpriseWms.Application;
using EnterpriseWms.Contracts;
using EnterpriseWms.Domain;
using EnterpriseWms.Infrastructure;
using Microsoft.IdentityModel.Tokens;

namespace EnterpriseWms.Api;

public sealed record JwtOptions(string Key);

public sealed class JwtTokenService
{
    private readonly JwtOptions _options;
    public JwtTokenService(JwtOptions options) => _options = options;
    public LoginResponse Create(CurrentUserDto user)
    {
        var expires = DateTime.UtcNow.AddHours(8);
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim("display_name", user.DisplayName),
            new Claim(ClaimTypes.Role, user.Role)
        };
        var credentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key)), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken("EnterpriseWms", "EnterpriseWms.WinForms", claims, expires: expires, signingCredentials: credentials);
        return new LoginResponse { Token = new JwtSecurityTokenHandler().WriteToken(token), ExpiresAtUtc = expires, User = user };
    }
}

public sealed class ApiExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiExceptionMiddleware> _logger;
    public ApiExceptionMiddleware(RequestDelegate next, ILogger<ApiExceptionMiddleware> logger) { _next = next; _logger = logger; }
    public async Task InvokeAsync(HttpContext context)
    {
        try { await _next(context); }
        catch (BusinessRuleException exception)
        {
            var status = exception.Code == "inventory.insufficient" || exception.Code.EndsWith("_exists", StringComparison.Ordinal) ? StatusCodes.Status409Conflict : StatusCodes.Status400BadRequest;
            await WriteProblemAsync(context, status, exception.Code, exception.Message);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unhandled API error for {Method} {Path}", context.Request.Method, context.Request.Path);
            await WriteProblemAsync(context, StatusCodes.Status500InternalServerError, "server.error", "服务器处理请求时发生错误。");
        }
    }
    private static async Task WriteProblemAsync(HttpContext context, int status, string code, string detail)
    {
        context.Response.StatusCode = status;
        await Results.Problem(statusCode: status, title: code, detail: detail, extensions: new Dictionary<string, object?> { ["code"] = code, ["traceId"] = context.TraceIdentifier }).ExecuteAsync(context);
    }
}

public sealed class OperationLogMiddleware
{
    private readonly RequestDelegate _next;
    public OperationLogMiddleware(RequestDelegate next) => _next = next;
    public async Task InvokeAsync(HttpContext context, IServiceScopeFactory scopeFactory)
    {
        if (HttpMethods.IsGet(context.Request.Method) || context.Request.Path.StartsWithSegments("/swagger") || context.Request.Path.StartsWithSegments("/health"))
        {
            await _next(context);
            return;
        }
        var watch = Stopwatch.StartNew();
        Exception? failure = null;
        try { await _next(context); }
        catch (Exception exception) { failure = exception; throw; }
        finally
        {
            watch.Stop();
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<WarehouseDbContext>();
                int? userId = int.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
                db.OperationLogs.Add(new OperationLog
                {
                    UserId = userId,
                    Username = context.User.Identity?.Name ?? "anonymous",
                    Module = context.Request.Path.Value?.Split('/', StringSplitOptions.RemoveEmptyEntries).ElementAtOrDefault(1) ?? "system",
                    Action = context.Request.Method,
                    Target = context.Request.Path,
                    Result = failure == null && context.Response.StatusCode < 400 ? "Success" : "Failed",
                    ElapsedMilliseconds = (int)watch.ElapsedMilliseconds,
                    IpAddress = context.Connection.RemoteIpAddress?.ToString() ?? string.Empty
                });
                await db.SaveChangesAsync(CancellationToken.None);
            }
            catch { /* 审计失败不能覆盖主业务响应；服务器日志仍会记录异常。 */ }
        }
    }
}

public static class ClaimsPrincipalExtensions
{
    public static int RequiredUserId(this ClaimsPrincipal principal) => int.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException());
}
