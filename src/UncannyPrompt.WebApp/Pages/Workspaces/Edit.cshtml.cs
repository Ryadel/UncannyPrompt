using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Localization;
using UncannyPrompt.Application;
using UncannyPrompt.WebApp.Security;

namespace UncannyPrompt.WebApp.Pages.Workspaces;

[Authorize]
public sealed class EditModel(
    ICurrentUserContext currentUser,
    IAppScopeService appScopeService,
    IWorkspaceService workspaceService,
    IStringLocalizerFactory localizerFactory) : PageModel
{
    private readonly IStringLocalizer localizer = localizerFactory.Create("Pages.Workspaces.Edit", typeof(EditModel).Assembly.GetName().Name!);

    [BindProperty(SupportsGet = true)]
    public Guid? Id { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid TenantId { get; set; }

    [BindProperty]
    public string Name { get; set; } = string.Empty;

    [BindProperty]
    public string? Description { get; set; }

    public IEnumerable<SelectListItem> TenantOptions { get; private set; } = [];
    public bool IsCreateMode => Id is null;
    public bool MustCreateTenantFirst => IsCreateMode && !TenantOptions.Any();
    public string Title => localizer[IsCreateMode ? "TitleCreate" : "TitleEdit"].Value;
    public string? ErrorMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var userId = RequireUser();
        var scopeState = await appScopeService.GetStateAsync(User, userId, cancellationToken);
        TenantOptions = scopeState.Tenants.Select(x => new SelectListItem(x.Name, x.Id.ToString())).ToList();

        if (Id is null)
        {
            TenantId = TenantId == Guid.Empty
                ? scopeState.CurrentTenantId ?? scopeState.Tenants.FirstOrDefault()?.Id ?? Guid.Empty
                : TenantId;
            return Page();
        }

        var workspace = (await workspaceService.ListAsync(userId, cancellationToken: cancellationToken))
            .FirstOrDefault(x => x.Id == Id.Value);
        if (workspace is null)
        {
            return RedirectToPage("/Workspaces");
        }

        TenantId = workspace.TenantId;
        Name = workspace.Name;
        Description = workspace.Description;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var userId = RequireUser();
        var scopeState = await appScopeService.GetStateAsync(User, userId, cancellationToken);
        TenantOptions = scopeState.Tenants.Select(x => new SelectListItem(x.Name, x.Id.ToString())).ToList();

        if (MustCreateTenantFirst || TenantId == Guid.Empty)
        {
            ErrorMessage = localizer["ErrorTenantRequired"].Value;
            return Page();
        }

        if (string.IsNullOrWhiteSpace(Name))
        {
            ErrorMessage = localizer["ErrorNameRequired"].Value;
            return Page();
        }

        if (Id is Guid workspaceId)
        {
            await workspaceService.UpdateAsync(userId, workspaceId, Name, Description, cancellationToken);
        }
        else
        {
            await workspaceService.CreateAsync(userId, TenantId, Name, Description, cancellationToken);
        }

        return RedirectToPage("/Workspaces");
    }

    private Guid RequireUser() => currentUser.UserId ?? throw new UnauthorizedAccessException();
}
