using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using UncannyPrompt.Application;
using UncannyPrompt.WebApp.Security;

namespace UncannyPrompt.WebApp.Pages.Developer.Api;

[Authorize]
public sealed class IndexModel(IUserApiKeyService apiKeyService, ICurrentUserContext currentUser) : PageModel
{
    [BindProperty]
    public Guid ApiKeyId { get; set; }

    public IReadOnlyList<ApiKeyDto> ApiKeys { get; private set; } = [];

    [TempData]
    public string? SuccessMessageKey { get; set; }

    [TempData]
    public string? ErrorMessageKey { get; set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadAsync(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostRevokeAsync(CancellationToken cancellationToken)
    {
        if (ApiKeyId == Guid.Empty)
        {
            ErrorMessageKey = "InvalidApiKey";
            return RedirectToPage("/Developer/Api/Index");
        }

        var revoked = await apiKeyService.RevokeAsync(RequireUser(), ApiKeyId, cancellationToken);
        if (revoked)
        {
            SuccessMessageKey = "RevokeSuccess";
        }
        else
        {
            ErrorMessageKey = "RevokeNotFound";
        }

        return RedirectToPage("/Developer/Api/Index");
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        ApiKeys = await apiKeyService.ListAsync(RequireUser(), cancellationToken);
    }

    private Guid RequireUser() => currentUser.UserId ?? throw new UnauthorizedAccessException();
}
