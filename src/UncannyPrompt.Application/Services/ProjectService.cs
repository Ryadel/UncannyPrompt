using Microsoft.EntityFrameworkCore;
using UncannyPrompt.Domain;
using UncannyPrompt.Shared;

namespace UncannyPrompt.Application;

internal sealed class ProjectService(IUnitOfWork unitOfWork, IPermissionService permissionService) : IProjectService
{
    public async Task<IReadOnlyList<ProjectDto>> ListAsync(Guid userId, Guid? workspaceId = null, CancellationToken cancellationToken = default)
    {
        var accessibleProjectIds = AccessControlQueries.AccessibleProjectIds(unitOfWork, userId, SharePermission.View);
        var projects =
            from project in unitOfWork.Repository<Project>().Query()
            where accessibleProjectIds.Contains(project.Id) && !project.IsDeleted && (workspaceId == null || project.WorkspaceId == workspaceId)
            orderby project.Name
            select project.ToDto();

        return await projects.Take(200).ToListAsync(cancellationToken);
    }

    public async Task<ProjectDto?> GetAsync(Guid userId, Guid projectId, CancellationToken cancellationToken = default)
    {
        var accessibleProjectIds = AccessControlQueries.AccessibleProjectIds(unitOfWork, userId, SharePermission.View);
        return await unitOfWork.Repository<Project>().Query()
            .Where(x => x.Id == projectId && !x.IsDeleted && accessibleProjectIds.Contains(x.Id))
            .Select(x => x.ToDto())
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<Guid, int>> CountByWorkspaceAsync(Guid userId, IReadOnlyCollection<Guid> workspaceIds, CancellationToken cancellationToken = default)
    {
        if (workspaceIds.Count == 0)
        {
            return new Dictionary<Guid, int>();
        }

        var accessibleProjectIds = AccessControlQueries.AccessibleProjectIds(unitOfWork, userId, SharePermission.View);
        return await unitOfWork.Repository<Project>().Query()
            .Where(x => !x.IsDeleted && workspaceIds.Contains(x.WorkspaceId) && accessibleProjectIds.Contains(x.Id))
            .GroupBy(x => x.WorkspaceId)
            .Select(x => new { WorkspaceId = x.Key, Count = x.Count() })
            .ToDictionaryAsync(x => x.WorkspaceId, x => x.Count, cancellationToken);
    }

    public async Task<ProjectDto> CreateAsync(Guid userId, Guid workspaceId, string name, string? description, CancellationToken cancellationToken = default)
    {
        var workspace = await unitOfWork.Repository<Workspace>().FindAsync(workspaceId, cancellationToken) ?? throw new InvalidOperationException("Workspace not found.");
        await permissionService.EnsureTenantAsync(userId, workspace.TenantId, ApplicationPermission.ProjectManage, cancellationToken);

        var project = new Project { WorkspaceId = workspaceId, Name = name.Trim(), Description = description };
        await unitOfWork.Repository<Project>().AddAsync(project, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return project.ToDto();
    }

    public async Task<ProjectDto> UpdateAsync(Guid userId, Guid projectId, string name, string? description, CancellationToken cancellationToken = default)
    {
        await permissionService.EnsureProjectAsync(userId, projectId, ApplicationPermission.ProjectManage, cancellationToken);

        var project = await unitOfWork.Repository<Project>().FindAsync(projectId, cancellationToken) ?? throw new InvalidOperationException("Project not found.");
        project.Name = name.Trim();
        project.Description = description;
        project.UpdatedAt = DateTimeOffset.UtcNow;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return project.ToDto();
    }

    public async Task DeleteAsync(Guid userId, Guid projectId, CancellationToken cancellationToken = default)
    {
        await permissionService.EnsureProjectAsync(userId, projectId, ApplicationPermission.ProjectManage, cancellationToken);

        var project = await unitOfWork.Repository<Project>().FindAsync(projectId, cancellationToken) ?? throw new InvalidOperationException("Project not found.");
        project.IsDeleted = true;
        project.DeletedAt = DateTimeOffset.UtcNow;
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
