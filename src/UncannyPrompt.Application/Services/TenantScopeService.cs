using UncannyPrompt.Domain;
using UncannyPrompt.Shared;

namespace UncannyPrompt.Application;

internal sealed class TenantScopeService(IUnitOfWork unitOfWork) : ITenantScopeService
{
    public Task<TenantScopeDto> GetDefaultScopeAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var accessibleProjectIds = AccessControlQueries.AccessibleProjectIds(unitOfWork, userId, SharePermission.View);
        var query =
            from project in unitOfWork.Repository<Project>().Query()
            join workspace in unitOfWork.Repository<Workspace>().Query() on project.WorkspaceId equals workspace.Id
            join tenant in unitOfWork.Repository<Tenant>().Query() on workspace.TenantId equals tenant.Id
            where accessibleProjectIds.Contains(project.Id) && !tenant.IsDeleted && !workspace.IsDeleted && !project.IsDeleted
            orderby tenant.Kind, tenant.CreatedAt, workspace.CreatedAt, project.CreatedAt
            select new TenantScopeDto(tenant.Id, tenant.Name, workspace.Id, workspace.Name, project.Id, project.Name);

        return Task.FromResult(query.FirstOrDefault() ?? throw new InvalidOperationException("No tenant scope is available for the current user."));
    }

    public Task<bool> CanAccessWorkspaceAsync(Guid userId, Guid workspaceId, SharePermission minimumPermission = SharePermission.View, CancellationToken cancellationToken = default)
    {
        var hasAccess = AccessControlQueries.AccessibleWorkspaceIds(unitOfWork, userId, minimumPermission).Contains(workspaceId);
        return Task.FromResult(hasAccess);
    }

    public Task<bool> CanAccessProjectAsync(Guid userId, Guid projectId, SharePermission minimumPermission = SharePermission.View, CancellationToken cancellationToken = default)
    {
        var hasAccess = AccessControlQueries.AccessibleProjectIds(unitOfWork, userId, minimumPermission).Contains(projectId);
        return Task.FromResult(hasAccess);
    }

    public async Task<bool> CanAccessFolderAsync(Guid userId, Guid folderId, SharePermission minimumPermission = SharePermission.View, CancellationToken cancellationToken = default)
    {
        var folder = await unitOfWork.Repository<Folder>().FindAsync(folderId, cancellationToken);
        if (folder is null || folder.IsDeleted)
        {
            return false;
        }

        if (await CanAccessProjectAsync(userId, folder.ProjectId, minimumPermission, cancellationToken))
        {
            return true;
        }

        var accessibleFolderIds = await AccessControlQueries.AccessibleFolderIdsAsync(unitOfWork, userId, minimumPermission, cancellationToken);
        return accessibleFolderIds.Contains(folderId);
    }

    public async Task<bool> CanAccessPromptAsync(Guid userId, Guid promptId, SharePermission minimumPermission = SharePermission.View, CancellationToken cancellationToken = default)
    {
        var prompt = await unitOfWork.Repository<Prompt>().FindAsync(promptId, cancellationToken);
        if (prompt is null || prompt.IsDeleted)
        {
            return false;
        }

        if (await CanAccessProjectAsync(userId, prompt.ProjectId, minimumPermission, cancellationToken))
        {
            return true;
        }

        if (AccessControlQueries.AccessiblePromptIds(unitOfWork, userId, minimumPermission).Contains(promptId))
        {
            return true;
        }

        if (prompt.FolderId is not { } folderId)
        {
            return false;
        }

        var accessibleFolderIds = await AccessControlQueries.AccessibleFolderIdsAsync(unitOfWork, userId, minimumPermission, cancellationToken);
        return accessibleFolderIds.Contains(folderId);
    }
}
