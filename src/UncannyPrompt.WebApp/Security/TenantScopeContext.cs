using UncannyPrompt.Domain;

namespace UncannyPrompt.WebApp.Security;

public sealed record TenantScopeContext(
    IReadOnlyList<Guid> AccessibleTenantIds,
    IReadOnlyList<Guid> EffectiveTenantIds,
    Guid? CurrentTenantId,
    Guid? CurrentWorkspaceId,
    Guid? CurrentProjectId,
    bool IsAllTenants,
    bool IsAdmin,
    TenantRole? CurrentTenantRole,
    bool CurrentTenantCanGrant)
{
    public bool CanAccessTenant(Guid tenantId) => AccessibleTenantIds.Contains(tenantId);

    public bool IsTenantInScope(Guid tenantId) => EffectiveTenantIds.Contains(tenantId);

    public bool CanManageMembers(Guid tenantId)
    {
        if (!IsTenantInScope(tenantId))
        {
            return false;
        }

        if (IsAdmin)
        {
            return true;
        }

        return CurrentTenantId == tenantId &&
               CurrentTenantRole is not null &&
               CurrentTenantRole <= TenantRole.TenantManager &&
               CurrentTenantCanGrant;
    }

    public bool CanManageWorkspaces(Guid tenantId)
    {
        if (!IsTenantInScope(tenantId))
        {
            return false;
        }

        if (IsAdmin)
        {
            return true;
        }

        return CurrentTenantId == tenantId &&
               CurrentTenantRole is not null &&
               CurrentTenantRole <= TenantRole.WorkspaceManager;
    }

    public bool CanManageProjects(Guid tenantId)
    {
        if (!IsTenantInScope(tenantId))
        {
            return false;
        }

        if (IsAdmin)
        {
            return true;
        }

        return CurrentTenantId == tenantId &&
               CurrentTenantRole is not null &&
               CurrentTenantRole <= TenantRole.ProjectContributor;
    }

    public bool CanUseChat(Guid tenantId)
    {
        if (!IsTenantInScope(tenantId))
        {
            return false;
        }

        if (IsAdmin)
        {
            return true;
        }

        return CurrentTenantId == tenantId &&
               CurrentTenantRole is not null &&
               CurrentTenantRole <= TenantRole.Reviewer;
    }
}
