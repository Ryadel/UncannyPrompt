using UncannyPrompt.Domain;

namespace UncannyPrompt.Application;

public interface IPermissionService
{
    Task<bool> CanAccessWorkspaceAsync(Guid userId, Guid workspaceId, ApplicationPermission permission, CancellationToken cancellationToken = default);
    Task<bool> CanAccessProjectAsync(Guid userId, Guid projectId, ApplicationPermission permission, CancellationToken cancellationToken = default);
    Task<bool> CanAccessFolderAsync(Guid userId, Guid folderId, ApplicationPermission permission, CancellationToken cancellationToken = default);
    Task<bool> CanAccessPromptAsync(Guid userId, Guid promptId, ApplicationPermission permission, CancellationToken cancellationToken = default);
    Task<bool> CanAccessTenantAsync(Guid userId, Guid tenantId, ApplicationPermission permission, CancellationToken cancellationToken = default);
    Task EnsureWorkspaceAsync(Guid userId, Guid workspaceId, ApplicationPermission permission, CancellationToken cancellationToken = default);
    Task EnsureProjectAsync(Guid userId, Guid projectId, ApplicationPermission permission, CancellationToken cancellationToken = default);
    Task EnsureFolderAsync(Guid userId, Guid folderId, ApplicationPermission permission, CancellationToken cancellationToken = default);
    Task EnsurePromptAsync(Guid userId, Guid promptId, ApplicationPermission permission, CancellationToken cancellationToken = default);
    Task EnsureTenantAsync(Guid userId, Guid tenantId, ApplicationPermission permission, CancellationToken cancellationToken = default);
}
