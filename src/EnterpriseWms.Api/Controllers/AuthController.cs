using System.Security.Claims;
using EnterpriseWms.Application;
using EnterpriseWms.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseWms.Api.Controllers;

[ApiController, Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _auth;
    private readonly JwtTokenService _tokens;
    public AuthController(IAuthService auth, JwtTokenService tokens) { _auth = auth; _tokens = tokens; }

    [AllowAnonymous, HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await _auth.ValidateCredentialsAsync(request.Username, request.Password, cancellationToken);
        return user == null ? Unauthorized(new { code = "auth.invalid_credentials", message = "用户名或密码错误。" }) : Ok(_tokens.Create(user));
    }

    [Authorize, HttpGet("me")]
    public ActionResult<CurrentUserDto> Me() => Ok(new CurrentUserDto
    {
        Id = User.RequiredUserId(),
        Username = User.Identity?.Name ?? string.Empty,
        DisplayName = User.FindFirstValue("display_name") ?? string.Empty,
        Role = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty
    });
}
