using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using UncannyPrompt.Application;

namespace UncannyPrompt.WebApp.Pages;

[Authorize]
public sealed class SettingsModel(
    ICurrentUserContext currentUser,
    IWorkspaceService workspaceService,
    IProjectService projectService) : PageModel
{
    public IReadOnlyList<WorkspaceDto> Workspaces { get; private set; } = [];
    public IReadOnlyList<ProjectDto> Projects { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadAsync(cancellationToken);
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        var userId = RequireUser();
        Workspaces = await workspaceService.ListAsync(userId, cancellationToken: cancellationToken);
        Projects = await projectService.ListAsync(userId, cancellationToken: cancellationToken);
    }

    private Guid RequireUser() => currentUser.UserId ?? throw new UnauthorizedAccessException();
}
