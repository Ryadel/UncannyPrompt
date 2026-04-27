using UncannyPrompt.Domain;

namespace UncannyPrompt.Application;

public interface ITenantScopeService
{
    Task<TenantScopeDto> GetDefaultScopeAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> CanAccessWorkspaceAsync(Guid userId, Guid workspaceId, SharePermission minimumPermission = SharePermission.View, CancellationToken cancellationToken = default);
    Task<bool> CanAccessProjectAsync(Guid userId, Guid projectId, SharePermission minimumPermission = SharePermission.View, CancellationToken cancellationToken = default);
    Task<bool> CanAccessFolderAsync(Guid userId, Guid folderId, SharePermission minimumPermission = SharePermission.View, CancellationToken cancellationToken = default);
    Task<bool> CanAccessPromptAsync(Guid userId, Guid promptId, SharePermission minimumPermission = SharePermission.View, CancellationToken cancellationToken = default);
}
