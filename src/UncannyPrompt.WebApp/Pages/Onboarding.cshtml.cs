using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using UncannyPrompt.Application;

namespace UncannyPrompt.WebApp.Pages;

[Authorize]
public sealed class OnboardingModel(
    ICurrentUserContext currentUser,
    IAppScopeService appScopeService,
    ITenantService tenantService,
    IWorkspaceService workspaceService,
    IProjectService projectService) : PageModel
{
    public TenantScopeDto Scope { get; private set; } = null!;
    public IReadOnlyList<TenantDto> Tenants { get; private set; } = [];
    public IReadOnlyList<WorkspaceDto> Workspaces { get; private set; } = [];
    public IReadOnlyList<ProjectDto> Projects { get; private set; } = [];

    [BindProperty]
    public TenantInput NewTenant { get; set; } = new();

    [BindProperty]
    public WorkspaceInput NewWorkspace { get; set; } = new();

    [BindProperty]
    public ProjectInput NewProject { get; set; } = new();

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostTenantAsync(CancellationToken cancellationToken)
    {
        var userId = RequireUser();
        if (!string.IsNullOrWhiteSpace(NewTenant.Name))
        {
            await tenantService.CreateCollaborativeAsync(userId, NewTenant.Name, cancellationToken);
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostWorkspaceAsync(CancellationToken cancellationToken)
    {
        var userId = RequireUser();
        if (NewWorkspace.TenantId != Guid.Empty && !string.IsNullOrWhiteSpace(NewWorkspace.Name))
        {
            await workspaceService.CreateAsync(userId, NewWorkspace.TenantId, NewWorkspace.Name, NewWorkspace.Description, cancellationToken);
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostProjectAsync(CancellationToken cancellationToken)
    {
        var userId = RequireUser();
        if (NewProject.WorkspaceId != Guid.Empty && !string.IsNullOrWhiteSpace(NewProject.Name))
        {
            await projectService.CreateAsync(userId, NewProject.WorkspaceId, NewProject.Name, NewProject.Description, cancellationToken);
        }

        return RedirectToPage();
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        var userId = RequireUser();
        var scopeState = await appScopeService.GetStateAsync(User, userId, cancellationToken);
        Scope = scopeState.DisplayScope;
        Tenants = scopeState.Tenants;
        Workspaces = scopeState.CurrentTenantId is Guid tenantId
            ? await workspaceService.ListAsync(userId, tenantId, cancellationToken)
            : [];
        Projects = scopeState.CurrentWorkspaceId is Guid workspaceId
            ? await projectService.ListAsync(userId, workspaceId, cancellationToken)
            : [];
    }

    private Guid RequireUser() => currentUser.UserId ?? throw new UnauthorizedAccessException();

    public sealed class TenantInput
    {
        public string Name { get; set; } = string.Empty;
    }

    public sealed class WorkspaceInput
    {
        public Guid TenantId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    public sealed class ProjectInput
    {
        public Guid WorkspaceId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}
