using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace UncannyPrompt.WebApp.Pages.Account;

[AllowAnonymous]
public sealed class LoginModel(IWebHostEnvironment environment, IConfiguration configuration) : PageModel
{
    public bool IsDevelopment => environment.IsDevelopment();
    public bool IsGoogleEnabled => HasClientCredentials("Authentication:Google");
    public bool IsEntraEnabled => HasClientCredentials("Authentication:EntraId") && !string.IsNullOrWhiteSpace(configuration["Authentication:EntraId:TenantId"]);
    public bool IsGitHubEnabled => HasClientCredentials("Authentication:GitHub");
    public bool HasExternalProviders => IsGoogleEnabled || IsEntraEnabled || IsGitHubEnabled;

    public string ReturnUrl { get; private set; } = "/";
    public string Intent { get; private set; } = "login";
    public bool HasError { get; private set; }
    public bool IsSignupIntent => string.Equals(Intent, "signup", StringComparison.OrdinalIgnoreCase);

    public void OnGet(string? returnUrl = null, string? error = null, string? intent = null)
    {
        ReturnUrl = string.IsNullOrWhiteSpace(returnUrl) ? "/Prompts" : returnUrl;
        Intent = string.Equals(intent, "signup", StringComparison.OrdinalIgnoreCase) ? "signup" : "login";
        HasError = !string.IsNullOrWhiteSpace(error);
    }

    private bool HasClientCredentials(string sectionName) =>
        !string.IsNullOrWhiteSpace(configuration[$"{sectionName}:ClientId"]) &&
        !string.IsNullOrWhiteSpace(configuration[$"{sectionName}:ClientSecret"]);
}
