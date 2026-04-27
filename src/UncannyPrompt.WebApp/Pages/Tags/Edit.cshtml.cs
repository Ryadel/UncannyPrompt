using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Localization;
using UncannyPrompt.Application;
using UncannyPrompt.Domain;
using UncannyPrompt.WebApp.Security;

namespace UncannyPrompt.WebApp.Pages.Tags;

[Authorize]
public sealed class EditModel(
    ICurrentUserContext currentUser,
    IAppScopeService appScopeService,
    ITagService tagService,
    IStringLocalizerFactory localizerFactory) : PageModel
{
    private readonly IStringLocalizer localizer = localizerFactory.Create("Pages.Tags.Edit", typeof(EditModel).Assembly.GetName().Name!);

    [BindProperty]
    public string Name { get; set; } = string.Empty;

    [BindProperty]
    public TagScope Scope { get; set; } = TagScope.Personal;

    [BindProperty]
    public Guid? TenantId { get; set; }

    [BindProperty]
    public Guid? WorkspaceId { get; set; }

    public IEnumerable<SelectListItem> WorkspaceOptions { get; private set; } = [];
    public string? ErrorMessage { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        await LoadAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(Name))
        {
            ErrorMessage = localizer["ErrorNameRequired"].Value;
            return Page();
        }

        await tagService.CreateAsync(RequireUser(), Name, Scope, TenantId, WorkspaceId, cancellationToken);
        return RedirectToPage("/Tags");
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        var userId = RequireUser();
        var scopeState = await appScopeService.GetStateAsync(User, userId, cancellationToken);
        TenantId = scopeState.CurrentTenantId;
        WorkspaceId ??= scopeState.CurrentWorkspaceId;
        WorkspaceOptions = scopeState.Workspaces
            .Select(x => new SelectListItem(x.Name, x.Id.ToString()))
            .ToList();
    }

    private Guid RequireUser() => currentUser.UserId ?? throw new UnauthorizedAccessException();
}
