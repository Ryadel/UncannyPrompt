using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;
using UncannyPrompt.Application;

namespace UncannyPrompt.WebApp.Pages.Tenants;

[Authorize]
public sealed class EditModel(
    ICurrentUserContext currentUser,
    ITenantService tenantService,
    IStringLocalizerFactory localizerFactory) : PageModel
{
    private readonly IStringLocalizer localizer = localizerFactory.Create("Pages.Tenants.Edit", typeof(EditModel).Assembly.GetName().Name!);

    [BindProperty]
    public string Name { get; set; } = string.Empty;

    public string? ErrorMessage { get; private set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            ErrorMessage = localizer["ErrorNameRequired"].Value;
            return Page();
        }

        await tenantService.CreateCollaborativeAsync(RequireUser(), Name, cancellationToken);
        return RedirectToPage("/Settings");
    }

    private Guid RequireUser() => currentUser.UserId ?? throw new UnauthorizedAccessException();
}
