using Microsoft.EntityFrameworkCore;
using UncannyPrompt.Domain;

namespace UncannyPrompt.Application;

internal sealed class PermissionService(IUnitOfWork unitOfWork, ITenantScopeService scopeService) : IPermissionService
{
    public async Task<bool> CanAccessWorkspaceAsync(Guid userId, Guid workspaceId, ApplicationPermission permission, CancellationToken cancellationToken = default)
    {
        if (await IsPlatformAdminAsync(userId, cancellationToken))
        {
            return await unitOfWork.Repository<Workspace>().Query()
                .AnyAsync(x => x.Id == workspaceId && !x.IsDeleted, cancellationToken);
        }

        var role = await (
                from workspace in unitOfWork.Repository<Workspace>().Query()
                join membership in unitOfWork.Repository<TenantMembership>().Query() on workspace.TenantId equals membership.TenantId
                where workspace.Id == workspaceId &&
                      !workspace.IsDeleted &&
                      membership.UserId == userId &&
                      membership.IsActive
                select (TenantRole?)membership.Role)
            .FirstOrDefaultAsync(cancellationToken);

        if (role is not null && RoleAllows(role.Value, permission))
        {
            return true;
        }

        return GrantAllows(permission) &&
               await scopeService.CanAccessWorkspaceAsync(userId, workspaceId, RequiredSharePermission(permission), cancellationToken);
    }

    public async Task<bool> CanAccessProjectAsync(Guid userId, Guid projectId, ApplicationPermission permission, CancellationToken cancellationToken = default)
    {
        if (await IsPlatformAdminAsync(userId, cancellationToken))
        {
            return await (
                    from project in unitOfWork.Repository<Project>().Query()
                    join workspace in unitOfWork.Repository<Workspace>().Query() on project.WorkspaceId equals workspace.Id
                    where project.Id == projectId && !project.IsDeleted && !workspace.IsDeleted
                    select project.Id)
                .AnyAsync(cancellationToken);
        }

        var role = await (
                from project in unitOfWork.Repository<Project>().Query()
                join workspace in unitOfWork.Repository<Workspace>().Query() on project.WorkspaceId equals workspace.Id
                join membership in unitOfWork.Repository<TenantMembership>().Query() on workspace.TenantId equals membership.TenantId
                where project.Id == projectId &&
                      !project.IsDeleted &&
                      !workspace.IsDeleted &&
                      membership.UserId == userId &&
                      membership.IsActive
                select (TenantRole?)membership.Role)
            .FirstOrDefaultAsync(cancellationToken);

        if (role is not null && RoleAllows(role.Value, permission))
        {
            return true;
        }

        return GrantAllows(permission) &&
               await scopeService.CanAccessProjectAsync(userId, projectId, RequiredSharePermission(permission), cancellationToken);
    }

    public async Task<bool> CanAccessFolderAsync(Guid userId, Guid folderId, ApplicationPermission permission, CancellationToken cancellationToken = default)
    {
        if (await IsPlatformAdminAsync(userId, cancellationToken))
        {
            return await (
                    from folder in unitOfWork.Repository<Folder>().Query()
                    join project in unitOfWork.Repository<Project>().Query() on folder.ProjectId equals project.Id
                    join workspace in unitOfWork.Repository<Workspace>().Query() on project.WorkspaceId equals workspace.Id
                    where folder.Id == folderId && !folder.IsDeleted && !project.IsDeleted && !workspace.IsDeleted
                    select folder.Id)
                .AnyAsync(cancellationToken);
        }

        var role = await (
                from folder in unitOfWork.Repository<Folder>().Query()
                join project in unitOfWork.Repository<Project>().Query() on folder.ProjectId equals project.Id
                join workspace in unitOfWork.Repository<Workspace>().Query() on project.WorkspaceId equals workspace.Id
                join membership in unitOfWork.Repository<TenantMembership>().Query() on workspace.TenantId equals membership.TenantId
                where folder.Id == folderId &&
                      !folder.IsDeleted &&
                      !project.IsDeleted &&
                      !workspace.IsDeleted &&
                      membership.UserId == userId &&
                      membership.IsActive
                select (TenantRole?)membership.Role)
            .FirstOrDefaultAsync(cancellationToken);

        if (role is not null && RoleAllows(role.Value, permission))
        {
            return true;
        }

        return GrantAllows(permission) &&
               await scopeService.CanAccessFolderAsync(userId, folderId, RequiredSharePermission(permission), cancellationToken);
    }

    public async Task<bool> CanAccessPromptAsync(Guid userId, Guid promptId, ApplicationPermission permission, CancellationToken cancellationToken = default)
    {
        if (await IsPlatformAdminAsync(userId, cancellationToken))
        {
            return await (
                    from prompt in unitOfWork.Repository<Prompt>().Query()
                    join project in unitOfWork.Repository<Project>().Query() on prompt.ProjectId equals project.Id
                    join workspace in unitOfWork.Repository<Workspace>().Query() on project.WorkspaceId equals workspace.Id
                    where prompt.Id == promptId && !prompt.IsDeleted && !project.IsDeleted && !workspace.IsDeleted
                    select prompt.Id)
                .AnyAsync(cancellationToken);
        }

        var role = await (
                from prompt in unitOfWork.Repository<Prompt>().Query()
                join project in unitOfWork.Repository<Project>().Query() on prompt.ProjectId equals project.Id
                join workspace in unitOfWork.Repository<Workspace>().Query() on project.WorkspaceId equals workspace.Id
                join membership in unitOfWork.Repository<TenantMembership>().Query() on workspace.TenantId equals membership.TenantId
                where prompt.Id == promptId &&
                      !prompt.IsDeleted &&
                      !project.IsDeleted &&
                      !workspace.IsDeleted &&
                      membership.UserId == userId &&
                      membership.IsActive
                select (TenantRole?)membership.Role)
            .FirstOrDefaultAsync(cancellationToken);

        if (role is not null && RoleAllows(role.Value, permission))
        {
            return true;
        }

        return GrantAllows(permission) &&
               await scopeService.CanAccessPromptAsync(userId, promptId, RequiredSharePermission(permission), cancellationToken);
    }

    public async Task<bool> CanAccessTenantAsync(Guid userId, Guid tenantId, ApplicationPermission permission, CancellationToken cancellationToken = default)
    {
        if (await IsPlatformAdminAsync(userId, cancellationToken))
        {
            return await unitOfWork.Repository<Tenant>().Query()
                .AnyAsync(x => x.Id == tenantId && !x.IsDeleted, cancellationToken);
        }

        var role = await unitOfWork.Repository<TenantMembership>().Query()
            .Where(x => x.UserId == userId && x.TenantId == tenantId && x.IsActive)
            .Select(x => (TenantRole?)x.Role)
            .FirstOrDefaultAsync(cancellationToken);

        return role is not null && RoleAllows(role.Value, permission);
    }

    public async Task EnsureProjectAsync(Guid userId, Guid projectId, ApplicationPermission permission, CancellationToken cancellationToken = default)
    {
        if (!await CanAccessProjectAsync(userId, projectId, permission, cancellationToken))
        {
            throw new UnauthorizedAccessException($"{permission} denied.");
        }
    }

    public async Task EnsureWorkspaceAsync(Guid userId, Guid workspaceId, ApplicationPermission permission, CancellationToken cancellationToken = default)
    {
        if (!await CanAccessWorkspaceAsync(userId, workspaceId, permission, cancellationToken))
        {
            throw new UnauthorizedAccessException($"{permission} denied.");
        }
    }

    public async Task EnsureFolderAsync(Guid userId, Guid folderId, ApplicationPermission permission, CancellationToken cancellationToken = default)
    {
        if (!await CanAccessFolderAsync(userId, folderId, permission, cancellationToken))
        {
            throw new UnauthorizedAccessException($"{permission} denied.");
        }
    }

    public async Task EnsurePromptAsync(Guid userId, Guid promptId, ApplicationPermission permission, CancellationToken cancellationToken = default)
    {
        if (!await CanAccessPromptAsync(userId, promptId, permission, cancellationToken))
        {
            throw new UnauthorizedAccessException($"{permission} denied.");
        }
    }

    public async Task EnsureTenantAsync(Guid userId, Guid tenantId, ApplicationPermission permission, CancellationToken cancellationToken = default)
    {
        if (!await CanAccessTenantAsync(userId, tenantId, permission, cancellationToken))
        {
            throw new UnauthorizedAccessException($"{permission} denied.");
        }
    }

    private static SharePermission RequiredSharePermission(ApplicationPermission permission) =>
        permission is ApplicationPermission.PromptView or ApplicationPermission.VersionView or ApplicationPermission.ExportContent
            ? SharePermission.View
            : SharePermission.Edit;

    private static bool GrantAllows(ApplicationPermission permission) =>
        permission is ApplicationPermission.PromptView
            or ApplicationPermission.PromptCreate
            or ApplicationPermission.PromptEdit
            or ApplicationPermission.VersionView
            or ApplicationPermission.VersionRestore
            or ApplicationPermission.FolderCreate
            or ApplicationPermission.FolderEdit
            or ApplicationPermission.TagManage
            or ApplicationPermission.VariableManage
            or ApplicationPermission.ExportContent;

    private static bool RoleAllows(TenantRole role, ApplicationPermission permission) =>
        permission switch
        {
            ApplicationPermission.PromptView or ApplicationPermission.VersionView or ApplicationPermission.ExportContent =>
                role is TenantRole.TenantOwner or TenantRole.TenantManager or TenantRole.WorkspaceManager or TenantRole.ProjectContributor or TenantRole.Reviewer or TenantRole.Viewer,

            ApplicationPermission.PromptCreate or ApplicationPermission.PromptEdit or ApplicationPermission.VersionRestore or
            ApplicationPermission.FolderCreate or ApplicationPermission.FolderEdit or ApplicationPermission.TagManage or ApplicationPermission.VariableManage =>
                role is TenantRole.TenantOwner or TenantRole.TenantManager or TenantRole.WorkspaceManager or TenantRole.ProjectContributor,

            ApplicationPermission.PromptDelete or ApplicationPermission.PromptRestore or ApplicationPermission.FolderDelete or
            ApplicationPermission.PromptPublish or ApplicationPermission.PromptShare or ApplicationPermission.ProjectManage =>
                role is TenantRole.TenantOwner or TenantRole.TenantManager or TenantRole.WorkspaceManager,

            ApplicationPermission.TenantManageMembers or ApplicationPermission.TenantManageRoles or ApplicationPermission.AuditView =>
                role is TenantRole.TenantOwner or TenantRole.TenantManager,

            _ => false
        };

    private Task<bool> IsPlatformAdminAsync(Guid userId, CancellationToken cancellationToken) =>
        unitOfWork.Repository<User>().Query()
            .AnyAsync(x => x.Id == userId && x.Status == UserStatus.Active && x.Role == UserRole.Admin, cancellationToken);
}
