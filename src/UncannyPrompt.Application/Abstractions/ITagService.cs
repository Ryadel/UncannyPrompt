using UncannyPrompt.Domain;

namespace UncannyPrompt.Application;

public interface ITagService
{
    Task<IReadOnlyList<TagDto>> ListAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<TagDto> CreateAsync(Guid userId, string name, TagScope scope = TagScope.Personal, Guid? tenantId = null, Guid? workspaceId = null, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid userId, Guid tagId, CancellationToken cancellationToken = default);
}
