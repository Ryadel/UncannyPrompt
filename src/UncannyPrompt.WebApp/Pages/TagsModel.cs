using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using UncannyPrompt.Application;
using UncannyPrompt.Domain;
using UncannyPrompt.WebApp.Security;

namespace UncannyPrompt.WebApp.Pages;

[Authorize]
public sealed class TagsModel(
    ICurrentUserContext currentUser,
    IAppScopeService appScopeService,
    IWorkspaceService workspaceService,
    ITagService tagService) : PageModel
{
    public TenantScopeDto Scope { get; private set; } = null!;
    public IReadOnlyList<WorkspaceDto> Workspaces { get; private set; } = [];
    public IReadOnlyList<TagDto> Tags { get; private set; } = [];

    [BindProperty]
    public TagInput Tag { get; set; } = new();

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostCreateAsync(CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(Tag.Name))
        {
            await tagService.CreateAsync(RequireUser(), Tag.Name, Tag.Scope, Tag.TenantId, Tag.WorkspaceId, cancellationToken);
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        await tagService.DeleteAsync(RequireUser(), id, cancellationToken);
        return RedirectToPage();
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        var userId = RequireUser();
        var scopeState = await appScopeService.GetStateAsync(User, userId, cancellationToken);
        Scope = scopeState.DisplayScope;
        Workspaces = scopeState.CurrentTenantId is Guid tenantId
            ? await workspaceService.ListAsync(userId, tenantId, cancellationToken)
            : [];
        Tags = await tagService.ListAsync(userId, cancellationToken);
        Tag.TenantId = scopeState.CurrentTenantId;
        Tag.WorkspaceId = scopeState.CurrentWorkspaceId;
    }

    private Guid RequireUser() => currentUser.UserId ?? throw new UnauthorizedAccessException();

    public sealed class TagInput
    {
        public string Name { get; set; } = string.Empty;
        public TagScope Scope { get; set; } = TagScope.Personal;
        public Guid? TenantId { get; set; }
        public Guid? WorkspaceId { get; set; }
    }
}
