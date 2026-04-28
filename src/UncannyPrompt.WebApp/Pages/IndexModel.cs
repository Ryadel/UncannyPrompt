using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using UncannyPrompt.Application;
using UncannyPrompt.WebApp.Security;

namespace UncannyPrompt.WebApp.Pages;

[AllowAnonymous]
public sealed class IndexModel(
    ICurrentUserContext currentUser,
    IAppScopeService appScopeService) : PageModel
{
    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return Page();
        }

        var userId = currentUser.UserId;
        if (userId is null)
        {
            return Page();
        }

        var scopeState = await appScopeService.GetStateAsync(User, userId.Value, cancellationToken);
        if (scopeState.CurrentWorkspaceId is null)
        {
            return RedirectToPage("/Workspaces");
        }
        if (scopeState.CurrentProjectId is null)
        {
            return RedirectToPage("/Projects");
        }
        return RedirectToPage("/Prompts");
    }
}
