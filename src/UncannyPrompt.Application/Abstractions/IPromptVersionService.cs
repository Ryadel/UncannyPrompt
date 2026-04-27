using UncannyPrompt.Domain;

namespace UncannyPrompt.Application;

public interface IPromptVersionService
{
    Task<IReadOnlyList<PromptVersionDto>> ListAsync(Guid userId, Guid promptId, CancellationToken cancellationToken = default);
    Task<PromptVersionDto> CreateAsync(Guid userId, Guid promptId, VersionCreateRequest request, CancellationToken cancellationToken = default);
    Task<PromptDto> RestoreAsync(Guid userId, Guid promptId, Guid versionId, string? changelog, CancellationToken cancellationToken = default);
    Task<string> DiffAsync(Guid userId, Guid leftVersionId, Guid rightVersionId, CancellationToken cancellationToken = default);
}
