using Core.Application.Security;
using Infrastructure.Security;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Presentation.API.Controllers;

public sealed record BrowserSessionResponse(string AccessToken, DateTime AccessTokenExpiresAt, UserResult User);

[ApiController]
[AllowAnonymous]
[Route("api/auth/session")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class BrowserSessionController(
    IAuthService authService,
    IAntiforgery antiforgery,
    IOptions<JwtOptions> jwtOptions,
    IWebHostEnvironment environment) : ControllerBase
{
    private const string CookieName = "daw.refresh";
    private const string CookiePath = "/api/auth/session";

    [HttpGet("csrf")]
    public ActionResult GetCsrfToken()
    {
        if (!IsSecureTransport()) return InsecureTransport();
        var tokens = antiforgery.GetAndStoreTokens(HttpContext);
        return Ok(new { requestToken = tokens.RequestToken });
    }

    [HttpPost("login")]
    public async Task<ActionResult<BrowserSessionResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var rejection = await ValidateRequestAsync();
        if (rejection is not null) return rejection;
        var response = await authService.LoginAsync(request, cancellationToken);
        if (Request.Cookies.TryGetValue(CookieName, out var previous))
            await authService.RevokeRefreshTokenAsync(previous, cancellationToken);
        SetRefreshCookie(response.RefreshToken);
        return Ok(ToBrowserResponse(response));
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<BrowserSessionResponse>> Refresh(CancellationToken cancellationToken)
    {
        var rejection = await ValidateRequestAsync();
        if (rejection is not null) return rejection;
        if (!Request.Cookies.TryGetValue(CookieName, out var token))
            return Problem(statusCode: 401, title: "Unauthorized", detail: "No browser session is available.");

        try
        {
            var response = await authService.RefreshAsync(new RefreshRequest(token), cancellationToken);
            SetRefreshCookie(response.RefreshToken);
            return Ok(ToBrowserResponse(response));
        }
        catch (UnauthorizedAccessException)
        {
            Response.Cookies.Delete(CookieName, CookieOptions());
            return Problem(statusCode: 401, title: "Unauthorized", detail: "The browser session is invalid or expired.");
        }
    }

    [HttpPost("logout")]
    public async Task<ActionResult> Logout(CancellationToken cancellationToken)
    {
        var rejection = await ValidateRequestAsync();
        if (rejection is not null) return rejection;
        if (Request.Cookies.TryGetValue(CookieName, out var token))
            await authService.LogoutAsync(token, cancellationToken);
        Response.Cookies.Delete(CookieName, CookieOptions());
        return NoContent();
    }

    private async Task<ActionResult?> ValidateRequestAsync()
    {
        if (!IsSecureTransport()) return InsecureTransport();
        try { await antiforgery.ValidateRequestAsync(HttpContext); }
        catch (AntiforgeryValidationException)
        {
            return Problem(statusCode: 400, title: "Bad Request", detail: "A valid CSRF token is required.");
        }
        return null;
    }

    private bool IsSecureTransport() => Request.IsHttps ||
        (environment.IsDevelopment() && IsLoopbackHost(Request.Host.Host));

    private static bool IsLoopbackHost(string host) =>
        string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase) ||
        (System.Net.IPAddress.TryParse(host, out var address) && System.Net.IPAddress.IsLoopback(address));

    private ActionResult InsecureTransport() =>
        Problem(statusCode: 400, title: "Bad Request", detail: "Browser sessions require HTTPS. HTTP is permitted only for loopback hosts in Development.");

    private CookieOptions CookieOptions() => new()
    {
        HttpOnly = true,
        Secure = !environment.IsDevelopment() || Request.IsHttps,
        SameSite = SameSiteMode.Strict,
        Path = CookiePath,
        IsEssential = true
    };

    private void SetRefreshCookie(string token)
    {
        var options = CookieOptions();
        options.Expires = DateTimeOffset.UtcNow.AddDays(jwtOptions.Value.RefreshTokenDays);
        options.MaxAge = TimeSpan.FromDays(jwtOptions.Value.RefreshTokenDays);
        Response.Cookies.Append(CookieName, token, options);
    }

    private static BrowserSessionResponse ToBrowserResponse(AuthResponse response) =>
        new(response.AccessToken, response.AccessTokenExpiresAt, response.User);
}
