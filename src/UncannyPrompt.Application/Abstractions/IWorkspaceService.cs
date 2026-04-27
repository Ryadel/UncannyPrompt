using UncannyPrompt.Domain;

namespace UncannyPrompt.Application;

public interface IWorkspaceService
{
    Task<IReadOnlyList<WorkspaceDto>> ListAsync(Guid userId, Guid? tenantId = null, CancellationToken cancellationToken = default);
    Task<WorkspaceDto?> GetAsync(Guid userId, Guid workspaceId, CancellationToken cancellationToken = default);
    Task<WorkspaceDto> CreateAsync(Guid userId, Guid tenantId, string name, string? description, CancellationToken cancellationToken = default);
    Task<WorkspaceDto> UpdateAsync(Guid userId, Guid workspaceId, string name, string? description, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid userId, Guid workspaceId, CancellationToken cancellationToken = default);
}
