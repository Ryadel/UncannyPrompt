using UncannyPrompt.Domain;

namespace UncannyPrompt.Application;

public interface IFolderService
{
    Task<IReadOnlyList<FolderDto>> ListAsync(Guid userId, Guid? projectId = null, CancellationToken cancellationToken = default);
    Task<FolderDto> CreateAsync(Guid userId, Guid projectId, Guid? parentFolderId, string name, CancellationToken cancellationToken = default);
    Task<FolderDto> UpdateAsync(Guid userId, Guid folderId, string name, Guid? parentFolderId, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid userId, Guid folderId, CancellationToken cancellationToken = default);
}
