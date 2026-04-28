using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using UncannyPrompt.Application;
using UncannyPrompt.WebApp.Security;

namespace UncannyPrompt.WebApp.Pages;

[Authorize]
public sealed class OnboardingModel(
    ICurrentUserContext currentUser,
    IAppScopeService appScopeService) : PageModel
{
    public string TenantName { get; private set; } = "Tenant";

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var userId = RequireUser();
        var scopeState = await appScopeService.GetStateAsync(User, userId, cancellationToken);
        TenantName = scopeState.CurrentTenantName
            ?? scopeState.Tenants.FirstOrDefault()?.Name
            ?? "Tenant";
    }

    private Guid RequireUser() => currentUser.UserId ?? throw new UnauthorizedAccessException();
}
