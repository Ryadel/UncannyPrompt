using System.Security.Claims;

namespace UncannyPrompt.WebApp.Security;

public interface IAppScopeService
{
    Task<AppScopeState> GetStateAsync(
        ClaimsPrincipal principal,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<AppScopeState?> SetStateAsync(
        ClaimsPrincipal principal,
        Guid userId,
        Guid tenantId,
        Guid? workspaceId,
        Guid? projectId,
        CancellationToken cancellationToken = default);
}