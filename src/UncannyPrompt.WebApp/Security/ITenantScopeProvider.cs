using System.Security.Claims;

namespace UncannyPrompt.WebApp.Security;

public interface ITenantScopeProvider
{
    Task<TenantScopeContext> GetScopeAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default);
    Task<bool> SetCurrentScopeAsync(ClaimsPrincipal principal, Guid tenantId, Guid? workspaceId, Guid? projectId, CancellationToken cancellationToken = default);
    Task<bool> SetCurrentTenantAsync(ClaimsPrincipal principal, Guid tenantId, CancellationToken cancellationToken = default);
    Task<bool> SetAllTenantsAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default);
    void ClearScope();
}
