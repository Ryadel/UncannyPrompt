using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using UncannyPrompt.Application;
using UncannyPrompt.WebApp.Security;

namespace UncannyPrompt.WebApp.Pages;

[Authorize]
public sealed class ProjectsModel(
    ICurrentUserContext currentUser,
    IAppScopeService appScopeService,
    IProjectService projectService,
    IFolderService folderService,
    IPromptService promptService) : PageModel
{
    public AppScopeState ScopeState { get; private set; } = new(null, null, null, null, null, null, [], [], []);
    public TenantScopeDto Scope { get; private set; } = null!;
    public IReadOnlyList<WorkspaceDto> Workspaces { get; private set; } = [];
    public IReadOnlyList<ProjectDto> Projects { get; private set; } = [];
    public IReadOnlyList<FolderDto> Folders { get; private set; } = [];
    public IReadOnlyDictionary<Guid, int> PromptCountsByProject { get; private set; } = new Dictionary<Guid, int>();

    public async Task<IActionResult> OnGetAsync(Guid? tenantId, Guid? workspaceId, CancellationToken cancellationToken)
    {
        if (tenantId is Guid selectedTenantId && workspaceId is Guid selectedWorkspaceId)
        {
            var state = await appScopeService.SetStateAsync(User, RequireUser(), selectedTenantId, selectedWorkspaceId, null, cancellationToken);
            return state?.CurrentWorkspaceId == selectedWorkspaceId
                ? RedirectToPage("/Projects")
                : RedirectToPage("/Workspaces");
        }

        await LoadAsync(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostDeleteProjectAsync(Guid id, CancellationToken cancellationToken)
    {
        await projectService.DeleteAsync(RequireUser(), id, cancellationToken);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteFolderAsync(Guid id, CancellationToken cancellationToken)
    {
        await folderService.DeleteAsync(RequireUser(), id, cancellationToken);
        return RedirectToPage();
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        var userId = RequireUser();
        ScopeState = await appScopeService.GetStateAsync(User, userId, cancellationToken);
        Scope = ScopeState.DisplayScope;
        Workspaces = ScopeState.Workspaces;
        Projects = ScopeState.CurrentWorkspaceId is Guid workspaceId
            ? await projectService.ListAsync(userId, workspaceId, cancellationToken)
            : [];
        Folders = Projects.Count == 0
            ? []
            : (await folderService.ListAsync(userId, cancellationToken: cancellationToken))
                .Where(x => Projects.Any(project => project.Id == x.ProjectId))
                .ToList();
        PromptCountsByProject = await promptService.CountByProjectAsync(userId, Projects.Select(x => x.Id).ToList(), cancellationToken);
    }

    private Guid RequireUser() => currentUser.UserId ?? throw new UnauthorizedAccessException();
}
