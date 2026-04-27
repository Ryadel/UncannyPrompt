using System.Security.Claims;
using UncannyPrompt.Application;

namespace UncannyPrompt.WebApp.Security;

public sealed class AppScopeService : IAppScopeService
{
    private readonly ITenantService tenantService;
    private readonly IWorkspaceService workspaceService;
    private readonly IProjectService projectService;
    private readonly ITenantScopeProvider tenantScopeProvider;

    public AppScopeService(
        ITenantService tenantService,
        IWorkspaceService workspaceService,
        IProjectService projectService,
        ITenantScopeProvider tenantScopeProvider)
    {
        this.tenantService = tenantService;
        this.workspaceService = workspaceService;
        this.projectService = projectService;
        this.tenantScopeProvider = tenantScopeProvider;
    }

    public async Task<AppScopeState> GetStateAsync(
        ClaimsPrincipal principal,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var context = await tenantScopeProvider.GetScopeAsync(principal, cancellationToken);
        var tenants = await tenantService.ListAsync(userId, cancellationToken);

        return await BuildStateAsync(
            userId,
            context.CurrentTenantId,
            context.CurrentWorkspaceId,
            context.CurrentProjectId,
            tenants,
            cancellationToken);
    }

    public async Task<AppScopeState?> SetStateAsync(
        ClaimsPrincipal principal,
        Guid userId,
        Guid tenantId,
        Guid? workspaceId,
        Guid? projectId,
        CancellationToken cancellationToken = default)
    {
        var tenants = await tenantService.ListAsync(userId, cancellationToken);
        if (!tenants.Any(tenant => tenant.Id == tenantId))
        {
            return null;
        }

        var state = await BuildStateAsync(userId, tenantId, workspaceId, projectId, tenants, cancellationToken);
        if (state.CurrentTenantId is not Guid resolvedTenantId)
        {
            return null;
        }

        var updated = await tenantScopeProvider.SetCurrentScopeAsync(
            principal,
            resolvedTenantId,
            state.CurrentWorkspaceId,
            state.CurrentProjectId,
            cancellationToken);

        return updated ? state : null;
    }

    private async Task<AppScopeState> BuildStateAsync(
        Guid userId,
        Guid? tenantId,
        Guid? workspaceId,
        Guid? projectId,
        IReadOnlyList<TenantDto> tenants,
        CancellationToken cancellationToken)
    {
        var currentTenant = ResolveCurrent(tenants, tenantId);
        var workspaces = currentTenant is null
            ? Array.Empty<WorkspaceDto>()
            : await workspaceService.ListAsync(userId, currentTenant.Id, cancellationToken);
        var currentWorkspace = ResolveCurrent(workspaces, workspaceId);
        var projects = currentWorkspace is null
            ? Array.Empty<ProjectDto>()
            : await projectService.ListAsync(userId, currentWorkspace.Id, cancellationToken);
        var currentProject = ResolveCurrent(projects, projectId);

        return new AppScopeState(
            currentTenant?.Id,
            currentTenant?.Name,
            currentWorkspace?.Id,
            currentWorkspace?.Name,
            currentProject?.Id,
            currentProject?.Name,
            tenants,
            workspaces,
            projects);
    }

    private static TItem? ResolveCurrent<TItem>(IReadOnlyList<TItem> items, Guid? requestedId)
        where TItem : class
    {
        if (items.Count == 0)
        {
            return null;
        }

        if (requestedId is Guid id)
        {
            var match = items.FirstOrDefault(item => GetId(item) == id);
            if (match is not null)
            {
                return match;
            }
        }

        return items[0];
    }

    private static Guid GetId<TItem>(TItem item)
    {
        return item switch
        {
            TenantDto tenant => tenant.Id,
            WorkspaceDto workspace => workspace.Id,
            ProjectDto project => project.Id,
            _ => Guid.Empty
        };
    }
}