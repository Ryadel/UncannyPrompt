using UncannyPrompt.Application;

namespace UncannyPrompt.WebApp.Security;

public sealed record AppScopeState(
    Guid? CurrentTenantId,
    string? CurrentTenantName,
    Guid? CurrentWorkspaceId,
    string? CurrentWorkspaceName,
    Guid? CurrentProjectId,
    string? CurrentProjectName,
    IReadOnlyList<TenantDto> Tenants,
    IReadOnlyList<WorkspaceDto> Workspaces,
    IReadOnlyList<ProjectDto> Projects)
{
    public TenantScopeDto? CurrentScope =>
        CurrentTenantId is Guid tenantId &&
        CurrentTenantName is not null &&
        CurrentWorkspaceId is Guid workspaceId &&
        CurrentWorkspaceName is not null &&
        CurrentProjectId is Guid projectId &&
        CurrentProjectName is not null
            ? new TenantScopeDto(tenantId, CurrentTenantName, workspaceId, CurrentWorkspaceName, projectId, CurrentProjectName)
            : null;

    public TenantScopeDto DisplayScope => new(
        CurrentTenantId ?? Guid.Empty,
        CurrentTenantName ?? "Tenant",
        CurrentWorkspaceId ?? Guid.Empty,
        CurrentWorkspaceName ?? "Workspace",
        CurrentProjectId ?? Guid.Empty,
        CurrentProjectName ?? "Project");
}