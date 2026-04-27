using UncannyPrompt.Domain;

namespace UncannyPrompt.Application;

public interface IProjectService
{
    Task<IReadOnlyList<ProjectDto>> ListAsync(Guid userId, Guid? workspaceId = null, CancellationToken cancellationToken = default);
    Task<ProjectDto?> GetAsync(Guid userId, Guid projectId, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<Guid, int>> CountByWorkspaceAsync(Guid userId, IReadOnlyCollection<Guid> workspaceIds, CancellationToken cancellationToken = default);
    Task<ProjectDto> CreateAsync(Guid userId, Guid workspaceId, string name, string? description, CancellationToken cancellationToken = default);
    Task<ProjectDto> UpdateAsync(Guid userId, Guid projectId, string name, string? description, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid userId, Guid projectId, CancellationToken cancellationToken = default);
}
