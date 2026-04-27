using System.Security.Claims;
using AspNet.Security.OAuth.GitHub;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UncannyPrompt.Application;
using UncannyPrompt.Shared;

namespace UncannyPrompt.WebApp.Controllers;

[Route("account")]
public sealed class AccountController(IUserService userService, IWebHostEnvironment environment, IConfiguration configuration) : Controller
{
    [HttpGet("signin/{provider}")]
    [AllowAnonymous]
    public IActionResult SignIn([FromRoute] string provider, [FromQuery] string? returnUrl = null)
    {
        var providerKey = provider.ToLowerInvariant();
        var scheme = providerKey switch
        {
            "google" => GoogleDefaults.AuthenticationScheme,
            "entra" or "microsoft" => OpenIdConnectDefaults.AuthenticationScheme,
            "github" => GitHubAuthenticationDefaults.AuthenticationScheme,
            _ => null
        };

        if (scheme is null || !IsProviderConfigured(providerKey))
        {
            return BadRequest("Unsupported or unconfigured provider.");
        }

        var properties = new AuthenticationProperties
        {
            RedirectUri = Url.Action(nameof(ExternalCallback), new { returnUrl = string.IsNullOrWhiteSpace(returnUrl) ? "/Prompts" : returnUrl })
        };
        return Challenge(properties, scheme);
    }

    [HttpGet("external-callback")]
    [AllowAnonymous]
    public IActionResult ExternalCallback([FromQuery] string? returnUrl = null) =>
        LocalRedirect(string.IsNullOrWhiteSpace(returnUrl) ? "/Prompts" : returnUrl);

    [HttpPost("dev-login")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DevLogin([FromForm] string email, [FromForm] string displayName, [FromForm] string? returnUrl = null, CancellationToken cancellationToken = default)
    {
        if (!environment.IsDevelopment())
        {
            return NotFound();
        }

        var user = await userService.ProvisionExternalUserAsync("development", email.Trim().ToLowerInvariant(), email, displayName, cancellationToken);
        await HttpContext.SignInAsync(AppConstants.CookieScheme, CreatePrincipal(user));
        return LocalRedirect(string.IsNullOrWhiteSpace(returnUrl) ? "/Prompts" : returnUrl);
    }

    [HttpPost("logout")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(AppConstants.CookieScheme);
        return LocalRedirect("/Account/Login");
    }

    public static ClaimsPrincipal CreatePrincipal(UserDto user)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.DisplayName),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };
        return new ClaimsPrincipal(new ClaimsIdentity(claims, AppConstants.CookieScheme));
    }

    private bool IsProviderConfigured(string provider) =>
        provider switch
        {
            "google" => HasClientCredentials("Authentication:Google"),
            "entra" or "microsoft" => HasClientCredentials("Authentication:EntraId") && !string.IsNullOrWhiteSpace(configuration["Authentication:EntraId:TenantId"]),
            "github" => HasClientCredentials("Authentication:GitHub"),
            _ => false
        };

    private bool HasClientCredentials(string sectionName) =>
        !string.IsNullOrWhiteSpace(configuration[$"{sectionName}:ClientId"]) &&
        !string.IsNullOrWhiteSpace(configuration[$"{sectionName}:ClientSecret"]);
}
