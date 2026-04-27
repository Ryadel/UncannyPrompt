using Microsoft.EntityFrameworkCore;
using UncannyPrompt.Domain;

namespace UncannyPrompt.Application;

internal static class AccessControlQueries
{
    public static IQueryable<Guid> AccessibleWorkspaceIds(IUnitOfWork unitOfWork, Guid userId, SharePermission minimumPermission)
    {
        var isPlatformAdmin = unitOfWork.Repository<User>().Query()
            .Any(x => x.Id == userId && x.Status == UserStatus.Active && x.Role == UserRole.Admin);

        var platformAdminWorkspaces = unitOfWork.Repository<Workspace>().Query()
            .Where(x => isPlatformAdmin && !x.IsDeleted)
            .Select(x => x.Id);

        var membershipWorkspaces =
            from workspace in unitOfWork.Repository<Workspace>().Query()
            join member in unitOfWork.Repository<TenantMembership>().Query() on workspace.TenantId equals member.TenantId
            where member.UserId == userId &&
                  member.IsActive &&
                  !workspace.IsDeleted &&
                  (minimumPermission == SharePermission.View ||
                   member.Role == TenantRole.TenantOwner ||
                   member.Role == TenantRole.TenantManager ||
                   member.Role == TenantRole.WorkspaceManager ||
                   member.Role == TenantRole.ProjectContributor)
            select workspace.Id;

        var grantWorkspaces = RecipientGrants(unitOfWork, userId, minimumPermission)
            .Where(x => x.TargetType == ShareTargetType.Workspace)
            .Select(x => x.TargetId);

        var projectScopedWorkspaces =
            from project in unitOfWork.Repository<Project>().Query()
            join workspace in unitOfWork.Repository<Workspace>().Query() on project.WorkspaceId equals workspace.Id
            where AccessibleProjectIds(unitOfWork, userId, minimumPermission).Contains(project.Id) &&
                  !project.IsDeleted &&
                  !workspace.IsDeleted
            select workspace.Id;

        return platformAdminWorkspaces
            .Union(membershipWorkspaces)
            .Union(grantWorkspaces)
            .Union(projectScopedWorkspaces);
    }

    public static IQueryable<Guid> AccessibleProjectIds(IUnitOfWork unitOfWork, Guid userId, SharePermission minimumPermission)
    {
        var isPlatformAdmin = unitOfWork.Repository<User>().Query()
            .Any(x => x.Id == userId && x.Status == UserStatus.Active && x.Role == UserRole.Admin);

        var platformAdminProjects =
            from project in unitOfWork.Repository<Project>().Query()
            join workspace in unitOfWork.Repository<Workspace>().Query() on project.WorkspaceId equals workspace.Id
            where isPlatformAdmin &&
                  !project.IsDeleted &&
                  !workspace.IsDeleted
            select project.Id;

        var membershipProjects =
            from project in unitOfWork.Repository<Project>().Query()
            join workspace in unitOfWork.Repository<Workspace>().Query() on project.WorkspaceId equals workspace.Id
            join member in unitOfWork.Repository<TenantMembership>().Query() on workspace.TenantId equals member.TenantId
            where member.UserId == userId &&
                  member.IsActive &&
                  !project.IsDeleted &&
                  !workspace.IsDeleted &&
                  (minimumPermission == SharePermission.View ||
                   member.Role == TenantRole.TenantOwner ||
                   member.Role == TenantRole.TenantManager ||
                   member.Role == TenantRole.WorkspaceManager ||
                   member.Role == TenantRole.ProjectContributor)
            select project.Id;

        var grantProjects = RecipientGrants(unitOfWork, userId, minimumPermission)
            .Where(x => x.TargetType == ShareTargetType.Project)
            .Select(x => x.TargetId);

        var workspaceGrantProjects =
            from project in unitOfWork.Repository<Project>().Query()
            where !project.IsDeleted &&
                  RecipientGrants(unitOfWork, userId, minimumPermission)
                      .Where(x => x.TargetType == ShareTargetType.Workspace)
                      .Select(x => x.TargetId)
                      .Contains(project.WorkspaceId)
            select project.Id;

        return platformAdminProjects.Union(membershipProjects).Union(grantProjects).Union(workspaceGrantProjects);
    }

    public static IQueryable<Guid> AccessiblePromptIds(IUnitOfWork unitOfWork, Guid userId, SharePermission minimumPermission) =>
        RecipientGrants(unitOfWork, userId, minimumPermission)
            .Where(x => x.TargetType == ShareTargetType.Prompt)
            .Select(x => x.TargetId);

    public static async Task<IReadOnlySet<Guid>> AccessibleFolderIdsAsync(IUnitOfWork unitOfWork, Guid userId, SharePermission minimumPermission, CancellationToken cancellationToken)
    {
        var grants = await RecipientGrants(unitOfWork, userId, minimumPermission)
            .Where(x => x.TargetType == ShareTargetType.Folder)
            .Select(x => new { x.TargetId, x.InheritsPermissions })
            .ToListAsync(cancellationToken);

        if (grants.Count == 0)
        {
            return new HashSet<Guid>();
        }

        var visibleFolderIds = grants.Select(x => x.TargetId).ToHashSet();
        var frontier = grants.Where(x => x.InheritsPermissions).Select(x => x.TargetId).ToHashSet();
        if (frontier.Count == 0)
        {
            return visibleFolderIds;
        }

        var folders = await unitOfWork.Repository<Folder>().Query()
            .Where(x => !x.IsDeleted)
            .Select(x => new { x.Id, x.ParentFolderId })
            .ToListAsync(cancellationToken);

        while (frontier.Count > 0)
        {
            var next = folders
                .Where(x => x.ParentFolderId is { } parentId && frontier.Contains(parentId) && visibleFolderIds.Add(x.Id))
                .Select(x => x.Id)
                .ToHashSet();

            frontier = next;
        }

        return visibleFolderIds;
    }

    public static IQueryable<ShareGrant> RecipientGrants(IUnitOfWork unitOfWork, Guid userId, SharePermission minimumPermission)
    {
        var tenantIds = ActiveTenantIds(unitOfWork, userId);
        var workspaceIds = ActiveWorkspaceIds(unitOfWork, userId);

        return unitOfWork.Repository<ShareGrant>().Query()
            .Where(x => !x.IsDeleted &&
                        x.Permission >= minimumPermission &&
                        (x.UserId == userId ||
                         (x.TenantId != null && tenantIds.Contains(x.TenantId.Value)) ||
                         (x.WorkspaceId != null && workspaceIds.Contains(x.WorkspaceId.Value))));
    }

    private static IQueryable<Guid> ActiveTenantIds(IUnitOfWork unitOfWork, Guid userId) =>
        unitOfWork.Repository<TenantMembership>().Query()
            .Where(x => x.UserId == userId && x.IsActive)
            .Select(x => x.TenantId);

    private static IQueryable<Guid> ActiveWorkspaceIds(IUnitOfWork unitOfWork, Guid userId)
    {
        var tenantIds = ActiveTenantIds(unitOfWork, userId);
        return unitOfWork.Repository<Workspace>().Query()
            .Where(x => !x.IsDeleted && tenantIds.Contains(x.TenantId))
            .Select(x => x.Id);
    }
}
