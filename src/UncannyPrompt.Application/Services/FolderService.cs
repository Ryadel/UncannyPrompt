using Microsoft.EntityFrameworkCore;
using UncannyPrompt.Domain;

namespace UncannyPrompt.Application;

internal sealed class FolderService(IUnitOfWork unitOfWork, IPermissionService permissionService) : IFolderService
{
    public async Task<IReadOnlyList<FolderDto>> ListAsync(Guid userId, Guid? projectId = null, CancellationToken cancellationToken = default)
    {
        var accessibleProjectIds = AccessControlQueries.AccessibleProjectIds(unitOfWork, userId, SharePermission.View);
        var accessibleFolderIds = await AccessControlQueries.AccessibleFolderIdsAsync(unitOfWork, userId, SharePermission.View, cancellationToken);
        var folders = await unitOfWork.Repository<Folder>().Query()
            .Where(x => !x.IsDeleted &&
                        (projectId == null || x.ProjectId == projectId) &&
                        (accessibleProjectIds.Contains(x.ProjectId) || accessibleFolderIds.Contains(x.Id)))
            .OrderBy(x => x.Name)
            .Take(500)
            .Select(x => x.ToDto())
            .ToListAsync(cancellationToken);

        return folders;
    }

    public async Task<FolderDto> CreateAsync(Guid userId, Guid projectId, Guid? parentFolderId, string name, CancellationToken cancellationToken = default)
    {
        await permissionService.EnsureProjectAsync(userId, projectId, ApplicationPermission.FolderCreate, cancellationToken);

        var folder = new Folder { ProjectId = projectId, ParentFolderId = parentFolderId, Name = name.Trim() };
        await unitOfWork.Repository<Folder>().AddAsync(folder, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return folder.ToDto();
    }

    public async Task<FolderDto> UpdateAsync(Guid userId, Guid folderId, string name, Guid? parentFolderId, CancellationToken cancellationToken = default)
    {
        var folder = await unitOfWork.Repository<Folder>().FindAsync(folderId, cancellationToken) ?? throw new InvalidOperationException("Folder not found.");
        await permissionService.EnsureFolderAsync(userId, folder.Id, ApplicationPermission.FolderEdit, cancellationToken);

        folder.Name = name.Trim();
        folder.ParentFolderId = parentFolderId;
        folder.UpdatedAt = DateTimeOffset.UtcNow;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return folder.ToDto();
    }

    public async Task DeleteAsync(Guid userId, Guid folderId, CancellationToken cancellationToken = default)
    {
        var folder = await unitOfWork.Repository<Folder>().FindAsync(folderId, cancellationToken) ?? throw new InvalidOperationException("Folder not found.");
        await permissionService.EnsureFolderAsync(userId, folder.Id, ApplicationPermission.FolderDelete, cancellationToken);

        folder.IsDeleted = true;
        folder.DeletedAt = DateTimeOffset.UtcNow;
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
