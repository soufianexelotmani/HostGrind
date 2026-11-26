using HostGrid.Api.DTOs.Auth;
using HostGrid.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace HostGrid.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
[EnableRateLimiting("authentication")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AuthController(AuthenticationService authentication, TokenService tokens,
    CurrentUserService current, AccountSecurityService security, IHostEnvironment environment) : ControllerBase
{
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken token)
        => Session(await authentication.LoginAsync(request, token));

    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponse>> Refresh(CancellationToken token)
        => Session(await tokens.RefreshAsync(Request.Cookies[TokenService.Cookie], token), clearOnFailure: true);

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken token)
    {
        await tokens.LogoutAsync(Request.Cookies[TokenService.Cookie], User, token);
        return ClearSession();
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<CurrentUserView>> Me(CancellationToken token)
        => await current.GetAsync(User, token) is { } user ? Ok(user) : Unauthorized();

    [Authorize]
    [HttpPost("password/change")]
    public async Task<IActionResult> ChangePassword(PasswordChangeRequest request, CancellationToken token)
    {
        await security.ChangePasswordAsync(User, request, token);
        return ClearSession();
    }

    [HttpPost("password/reset-request")]
    public async Task<IActionResult> RequestReset(EmailRequest request, CancellationToken token)
    {
        await security.RequestResetAsync(request.Email, token);
        return Accepted();
    }

    [HttpPost("password/reset")]
    public async Task<IActionResult> ResetPassword(PasswordLinkRequest request, CancellationToken token)
    {
        await security.CompleteLinkAsync(request, false, token);
        return ClearSession();
    }

    [HttpPost("activate")]
    public async Task<IActionResult> Activate(PasswordLinkRequest request, CancellationToken token)
    {
        await security.CompleteLinkAsync(request, true, token);
        return NoContent();
    }

    [Authorize]
    [HttpPost("mfa/start")]
    public Task<AuthResponse> StartMfa(MfaStartRequest request, CancellationToken token)
        => authentication.StartOptionalEnrollmentAsync(User, request.Password, token);

    [HttpPost("mfa/enrollment")]
    public Task<EnrollmentView> Enrollment(MfaChallengeRequest request, CancellationToken token)
        => authentication.EnrollmentAsync(request.Challenge, token);

    [HttpPost("mfa/verify")]
    public async Task<ActionResult<AuthResponse>> VerifyMfa(MfaVerifyRequest request, CancellationToken token)
        => Session(await authentication.VerifyMfaAsync(request, token));

    [Authorize]
    [HttpPost("mfa/reset")]
    public async Task<IActionResult> ResetMfa(MfaResetRequest request, CancellationToken token)
    {
        await authentication.ResetMfaAsync(User, request, token);
        return ClearSession();
    }

    private IActionResult ClearSession()
    {
        Response.Cookies.Delete(TokenService.Cookie, CookieOptions());
        return NoContent();
    }

    private ActionResult<AuthResponse> Session(SessionResult? result, bool clearOnFailure = false)
    {
        if (result is null)
        {
            if (clearOnFailure) Response.Cookies.Delete(TokenService.Cookie, CookieOptions());
            return Problem(statusCode: 401, title: "Authentication could not be verified.");
        }
        if (result.RefreshToken is not null)
        {
            var cookie = CookieOptions();
            cookie.MaxAge = TokenService.RefreshLifetime;
            Response.Cookies.Append(TokenService.Cookie, result.RefreshToken, cookie);
        }
        return Ok(result.Response);
    }

    private CookieOptions CookieOptions() => new()
    {
        HttpOnly = true, Secure = !environment.IsDevelopment() || Request.IsHttps,
        SameSite = SameSiteMode.Strict, Path = "/api/v1/auth", IsEssential = true
    };
}
