using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using UncannyPrompt.Application;
using UncannyPrompt.Domain;
using UncannyPrompt.WebApp.Security;

namespace UncannyPrompt.WebApp.Pages;

[Authorize]
public sealed class VariablesModel(
    ICurrentUserContext currentUser,
    IAppScopeService appScopeService,
    IVariableService variableService) : PageModel
{
    public TenantScopeDto Scope { get; private set; } = null!;
    public IReadOnlyList<ProjectDto> Projects { get; private set; } = [];
    public IReadOnlyList<VariableDto> Variables { get; private set; } = [];

    [BindProperty(SupportsGet = true)]
    public Guid? ProjectId { get; set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        return await LoadAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        await variableService.DeleteAsync(RequireUser(), id, cancellationToken);
        return RedirectToPage(new { projectId = ProjectId });
    }

    private async Task<IActionResult> LoadAsync(CancellationToken cancellationToken)
    {
        var userId = RequireUser();
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
        ProjectId ??= Scope.ProjectId;
        Projects = scopeState.Projects;
        Variables = await variableService.ListAsync(userId, ProjectId, cancellationToken);
        return Page();
    }

    private Guid RequireUser() => currentUser.UserId ?? throw new UnauthorizedAccessException();
}
