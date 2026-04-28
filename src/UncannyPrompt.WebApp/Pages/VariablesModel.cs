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
    IProjectService projectService,
    IVariableService variableService) : PageModel
{
    public TenantScopeDto Scope { get; private set; } = null!;
    public IReadOnlyList<ProjectDto> Projects { get; private set; } = [];
    public IReadOnlyList<VariableDto> Variables { get; private set; } = [];

    [BindProperty(SupportsGet = true)]
    public Guid? ProjectId { get; set; }

    [BindProperty]
    public VariableInput Variable { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        return await LoadAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostSaveAsync(CancellationToken cancellationToken)
    {
        var projectId = Variable.Scope == VariableScope.Project ? Variable.ProjectId : null;
        await variableService.UpsertAsync(
            RequireUser(),
            new VariableUpsertRequest(
                Variable.Id,
                Variable.Name,
                Variable.Description,
                Variable.DefaultValue,
                Variable.IsRequired,
                Variable.IsSecret,
                Variable.Scope,
                projectId,
                Variable.Value),
            cancellationToken);

        return RedirectToPage(new { projectId = projectId ?? ProjectId });
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
        Variable.ProjectId = ProjectId;
        Variable.Scope = VariableScope.Project;
        return Page();
    }

    private Guid RequireUser() => currentUser.UserId ?? throw new UnauthorizedAccessException();

    public sealed class VariableInput
    {
        public Guid? Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? DefaultValue { get; set; }
        public bool IsRequired { get; set; }
        public bool IsSecret { get; set; }
        public VariableScope Scope { get; set; } = VariableScope.Project;
        public Guid? ProjectId { get; set; }
        public string? Value { get; set; }
    }
}
