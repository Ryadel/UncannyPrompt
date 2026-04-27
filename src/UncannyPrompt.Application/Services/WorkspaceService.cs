using Microsoft.EntityFrameworkCore;
using UncannyPrompt.Domain;
using UncannyPrompt.Shared;

namespace UncannyPrompt.Application;

internal sealed class WorkspaceService(IUnitOfWork unitOfWork, IPermissionService permissionService) : IWorkspaceService
{
    public async Task<IReadOnlyList<WorkspaceDto>> ListAsync(Guid userId, Guid? tenantId = null, CancellationToken cancellationToken = default)
    {
        var accessibleWorkspaceIds = AccessControlQueries.AccessibleWorkspaceIds(unitOfWork, userId, SharePermission.View);
        var query = unitOfWork.Repository<Workspace>().Query()
            .Where(x => !x.IsDeleted && accessibleWorkspaceIds.Contains(x.Id) && (tenantId == null || x.TenantId == tenantId))
            .OrderBy(x => x.Name)
            .Select(x => x.ToDto());

        return await query.Take(200).ToListAsync(cancellationToken);
    }

    public async Task<WorkspaceDto?> GetAsync(Guid userId, Guid workspaceId, CancellationToken cancellationToken = default)
    {
        var accessibleWorkspaceIds = AccessControlQueries.AccessibleWorkspaceIds(unitOfWork, userId, SharePermission.View);
        return await unitOfWork.Repository<Workspace>().Query()
            .Where(x => x.Id == workspaceId && !x.IsDeleted && accessibleWorkspaceIds.Contains(x.Id))
            .Select(x => x.ToDto())
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<WorkspaceDto> CreateAsync(Guid userId, Guid tenantId, string name, string? description, CancellationToken cancellationToken = default)
    {
        await permissionService.EnsureTenantAsync(userId, tenantId, ApplicationPermission.ProjectManage, cancellationToken);

        var workspace = new Workspace { TenantId = tenantId, Name = name.Trim(), Description = description };
        await unitOfWork.Repository<Workspace>().AddAsync(workspace, cancellationToken);

        var user = await unitOfWork.Repository<User>().FindAsync(userId, cancellationToken);
        if (user is { OnboardingDismissedAt: null })
        {
            user.OnboardingDismissedAt = DateTimeOffset.UtcNow;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return workspace.ToDto();
    }

    public async Task<WorkspaceDto> UpdateAsync(Guid userId, Guid workspaceId, string name, string? description, CancellationToken cancellationToken = default)
    {
        await permissionService.EnsureWorkspaceAsync(userId, workspaceId, ApplicationPermission.ProjectManage, cancellationToken);

        var workspace = await unitOfWork.Repository<Workspace>().FindAsync(workspaceId, cancellationToken) ?? throw new InvalidOperationException("Workspace not found.");
        workspace.Name = name.Trim();
        workspace.Description = description;
        workspace.UpdatedAt = DateTimeOffset.UtcNow;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return workspace.ToDto();
    }

    public async Task DeleteAsync(Guid userId, Guid workspaceId, CancellationToken cancellationToken = default)
    {
        await permissionService.EnsureWorkspaceAsync(userId, workspaceId, ApplicationPermission.ProjectManage, cancellationToken);

        var workspace = await unitOfWork.Repository<Workspace>().FindAsync(workspaceId, cancellationToken) ?? throw new InvalidOperationException("Workspace not found.");
        workspace.IsDeleted = true;
        workspace.DeletedAt = DateTimeOffset.UtcNow;
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
