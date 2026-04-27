using UncannyPrompt.Domain;

namespace UncannyPrompt.Application;

public interface IPromptService
{
    Task<IReadOnlyList<PromptDto>> ListAsync(Guid userId, PromptSearchRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<Guid, int>> CountByProjectAsync(Guid userId, IReadOnlyCollection<Guid> projectIds, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<Guid, int>> CountByWorkspaceAsync(Guid userId, IReadOnlyCollection<Guid> workspaceIds, CancellationToken cancellationToken = default);
    Task<PromptDto?> GetAsync(Guid userId, Guid promptId, CancellationToken cancellationToken = default);
    Task<PromptDto> CreateAsync(Guid userId, PromptUpsertRequest request, CancellationToken cancellationToken = default);
    Task<PromptDto> UpdateAsync(Guid userId, Guid promptId, PromptUpsertRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid userId, Guid promptId, CancellationToken cancellationToken = default);
    Task<PromptDto> ToggleFavoriteAsync(Guid userId, Guid promptId, CancellationToken cancellationToken = default);
    Task<PromptDto> TogglePinnedAsync(Guid userId, Guid promptId, CancellationToken cancellationToken = default);
    Task LogCopyAsync(Guid userId, Guid promptId, bool resolved, CancellationToken cancellationToken = default);
}
