using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using UncannyPrompt.Application;
using UncannyPrompt.Domain;
using UncannyPrompt.WebApp.Security;

namespace UncannyPrompt.WebApp.Pages;

[Authorize]
public sealed class SharesModel(
    ICurrentUserContext currentUser,
    IAppScopeService appScopeService,
    IPromptService promptService,
    IProjectService projectService,
    IFolderService folderService,
    ITenantService tenantService,
    IWorkspaceService workspaceService,
    ISharingService sharingService) : PageModel
{
    public TenantScopeDto Scope { get; private set; } = null!;
    public IReadOnlyList<PromptDto> Prompts { get; private set; } = [];
    public IReadOnlyList<ProjectDto> Projects { get; private set; } = [];
    public IReadOnlyList<FolderDto> Folders { get; private set; } = [];
    public IReadOnlyList<TenantDto> Tenants { get; private set; } = [];
    public IReadOnlyList<WorkspaceDto> Workspaces { get; private set; } = [];
    public IReadOnlyList<ShareGrantDto> Grants { get; private set; } = [];

    [TempData]
    public string? PublicToken { get; set; }

    [BindProperty]
    public GrantInput Grant { get; set; } = new();

    [BindProperty]
    public PublicLinkInput PublicLink { get; set; } = new();

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostGrantAsync(CancellationToken cancellationToken)
    {
        await sharingService.GrantAsync(
            RequireUser(),
            new ShareGrantRequest(
                Grant.TargetType,
                Grant.TargetId,
                Grant.UserId,
                Grant.TenantId,
                Grant.WorkspaceId,
                Grant.Permission),
            cancellationToken);

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteGrantAsync(Guid id, CancellationToken cancellationToken)
    {
        await sharingService.DeleteGrantAsync(RequireUser(), id, cancellationToken);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostPublicLinkAsync(CancellationToken cancellationToken)
    {
        if (PublicLink.PromptId != Guid.Empty)
        {
            var link = await sharingService.CreatePublicLinkAsync(RequireUser(), PublicLink.PromptId, PublicLink.ExpiresAt, cancellationToken);
            PublicToken = link.Token;
        }

        return RedirectToPage();
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        var userId = RequireUser();
        var scopeState = await appScopeService.GetStateAsync(User, userId, cancellationToken);
        Scope = scopeState.DisplayScope;
        Projects = scopeState.CurrentWorkspaceId is Guid workspaceId
            ? await projectService.ListAsync(userId, workspaceId, cancellationToken)
            : [];
        Folders = scopeState.CurrentWorkspaceId is Guid
            ? await folderService.ListAsync(userId, cancellationToken: cancellationToken)
            : [];
        Prompts = scopeState.CurrentProjectId is Guid
            ? await promptService.ListAsync(userId, new PromptSearchRequest(ProjectId: scopeState.CurrentProjectId, PageSize: 100), cancellationToken)
            : [];
        Tenants = await tenantService.ListAsync(userId, cancellationToken);
        Workspaces = scopeState.CurrentTenantId is Guid tenantId
            ? await workspaceService.ListAsync(userId, tenantId, cancellationToken)
            : [];
        Grants = await sharingService.ListAsync(userId, cancellationToken: cancellationToken);
        Grant.TargetType = ShareTargetType.Prompt;
        Grant.Permission = SharePermission.View;
    }

    private Guid RequireUser() => currentUser.UserId ?? throw new UnauthorizedAccessException();

    public sealed class GrantInput
    {
        public ShareTargetType TargetType { get; set; } = ShareTargetType.Prompt;
        public Guid TargetId { get; set; }
        public Guid? UserId { get; set; }
        public Guid? TenantId { get; set; }
        public Guid? WorkspaceId { get; set; }
        public SharePermission Permission { get; set; } = SharePermission.View;
    }

    public sealed class PublicLinkInput
    {
        public Guid PromptId { get; set; }
        public DateTimeOffset? ExpiresAt { get; set; }
    }
}
