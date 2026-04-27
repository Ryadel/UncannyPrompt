using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;
using UncannyPrompt.Application;
using UncannyPrompt.Domain;
using UncannyPrompt.WebApp.Security;

namespace UncannyPrompt.WebApp.Pages.Shares.Grant;

[Authorize]
public sealed class EditModel(
    ICurrentUserContext currentUser,
    IAppScopeService appScopeService,
    IPromptService promptService,
    IProjectService projectService,
    IFolderService folderService,
    ITenantService tenantService,
    IWorkspaceService workspaceService,
    ISharingService sharingService,
    IStringLocalizerFactory localizerFactory) : PageModel
{
    private readonly IStringLocalizer localizer = localizerFactory.Create("Pages.Shares.Grant.Edit", typeof(EditModel).Assembly.GetName().Name!);

    public IReadOnlyList<PromptDto> Prompts { get; private set; } = [];
    public IReadOnlyList<ProjectDto> Projects { get; private set; } = [];
    public IReadOnlyList<FolderDto> Folders { get; private set; } = [];
    public IReadOnlyList<TenantDto> Tenants { get; private set; } = [];
    public IReadOnlyList<WorkspaceDto> Workspaces { get; private set; } = [];

    [BindProperty]
    public ShareTargetType TargetType { get; set; } = ShareTargetType.Prompt;

    [BindProperty]
    public Guid TargetId { get; set; }

    [BindProperty]
    public Guid? UserId { get; set; }

    [BindProperty]
    public Guid? TenantId { get; set; }

    [BindProperty]
    public Guid? WorkspaceId { get; set; }

    [BindProperty]
    public SharePermission Permission { get; set; } = SharePermission.View;

    public string? ErrorMessage { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadAsync(cancellationToken);
        if (Prompts.FirstOrDefault() is { } prompt)
        {
            TargetType = ShareTargetType.Prompt;
            TargetId = prompt.Id;
        }
        else if (Projects.FirstOrDefault() is { } project)
        {
            TargetType = ShareTargetType.Project;
            TargetId = project.Id;
        }
        else if (Folders.FirstOrDefault() is { } folder)
        {
            TargetType = ShareTargetType.Folder;
            TargetId = folder.Id;
        }
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        await LoadAsync(cancellationToken);
        if (TargetId == Guid.Empty)
        {
            ErrorMessage = localizer["ErrorTargetRequired"].Value;
            return Page();
        }

        await sharingService.GrantAsync(
            RequireUser(),
            new ShareGrantRequest(TargetType, TargetId, UserId, TenantId, WorkspaceId, Permission),
            cancellationToken);

        return RedirectToPage("/Shares");
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        var userId = RequireUser();
        var scopeState = await appScopeService.GetStateAsync(User, userId, cancellationToken);
        Projects = scopeState.CurrentWorkspaceId is Guid workspaceId
            ? await projectService.ListAsync(userId, workspaceId, cancellationToken)
            : [];
        Folders = scopeState.CurrentWorkspaceId is Guid
            ? await folderService.ListAsync(userId, cancellationToken: cancellationToken)
            : [];
        Prompts = scopeState.CurrentProjectId is Guid projectId
            ? await promptService.ListAsync(userId, new PromptSearchRequest(ProjectId: projectId, PageSize: 100), cancellationToken)
            : [];
        Tenants = await tenantService.ListAsync(userId, cancellationToken);
        Workspaces = scopeState.CurrentTenantId is Guid tenantId
            ? await workspaceService.ListAsync(userId, tenantId, cancellationToken)
            : [];
    }

    private Guid RequireUser() => currentUser.UserId ?? throw new UnauthorizedAccessException();
}
