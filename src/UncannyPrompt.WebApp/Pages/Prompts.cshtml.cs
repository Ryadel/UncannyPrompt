using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using UncannyPrompt.Application;
using UncannyPrompt.WebApp.Security;

namespace UncannyPrompt.WebApp.Pages;

[Authorize]
public sealed class PromptsModel(
    ICurrentUserContext currentUser,
    IAppScopeService appScopeService,
    IFolderService folderService,
    ITagService tagService,
    IVariableService variableService) : PageModel
{
    public TenantScopeDto Scope { get; private set; } = null!;
    public IReadOnlyList<ProjectDto> Projects { get; private set; } = [];
    public IReadOnlyList<FolderDto> Folders { get; private set; } = [];
    public IReadOnlyList<TagDto> Tags { get; private set; } = [];
    public IReadOnlyList<VariableDto> Variables { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(Guid? tenantId, Guid? workspaceId, Guid? projectId, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedAccessException();

        if (tenantId is Guid selectedTenantId && workspaceId is Guid selectedWorkspaceId && projectId is Guid selectedProjectId)
        {
            var selectedState = await appScopeService.SetStateAsync(User, userId, selectedTenantId, selectedWorkspaceId, selectedProjectId, cancellationToken);
            return selectedState?.CurrentProjectId == selectedProjectId
                ? RedirectToPage("/Prompts")
                : RedirectToPage("/Projects");
        }

        var scopeState = await appScopeService.GetStateAsync(User, userId, cancellationToken);
        if (scopeState.CurrentWorkspaceId is null)
        {
            return RedirectToPage("/Workspaces");
        }
        if (scopeState.CurrentProjectId is null)
        {
            return RedirectToPage("/Projects");
        }
        if (scopeState.CurrentScope is null)
        {
            return RedirectToPage("/Workspaces");
        }

        Scope = scopeState.CurrentScope;
        Projects = scopeState.Projects;
        Folders = await folderService.ListAsync(userId, Scope.ProjectId, cancellationToken);
        Tags = await tagService.ListAsync(userId, cancellationToken);
        Variables = await variableService.ListAsync(userId, Scope.ProjectId, cancellationToken);
        return Page();
    }
}
