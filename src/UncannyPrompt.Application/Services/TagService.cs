using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using UncannyPrompt.Domain;

namespace UncannyPrompt.Application;

internal sealed class TagService(IUnitOfWork unitOfWork, IPermissionService permissionService) : ITagService
{
    public async Task<IReadOnlyList<TagDto>> ListAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var tags = await unitOfWork.Repository<Tag>().Query()
            .Where(x => x.Scope == TagScope.System || x.UserId == userId)
            .OrderBy(x => x.Name)
            .Take(500)
            .Select(x => x.ToDto())
            .ToListAsync(cancellationToken);
        return tags;
    }

    public async Task<TagDto> CreateAsync(Guid userId, string name, TagScope scope = TagScope.Personal, Guid? tenantId = null, Guid? workspaceId = null, CancellationToken cancellationToken = default)
    {
        await EnsureCanManageScopeAsync(userId, scope, tenantId, workspaceId, cancellationToken);

        var normalized = name.Trim().ToLowerInvariant();
        var tag = await unitOfWork.Repository<Tag>().Query()
            .FirstOrDefaultAsync(x => x.UserId == userId && x.Name == normalized, cancellationToken)
            ?? new Tag { UserId = userId, Name = normalized, Scope = scope, TenantId = tenantId, WorkspaceId = workspaceId };
        if (!await unitOfWork.Repository<Tag>().Query().AnyAsync(x => x.Id == tag.Id, cancellationToken))
        {
            await unitOfWork.Repository<Tag>().AddAsync(tag, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return tag.ToDto();
    }

    public async Task DeleteAsync(Guid userId, Guid tagId, CancellationToken cancellationToken = default)
    {
        var tag = await unitOfWork.Repository<Tag>().FindAsync(tagId, cancellationToken) ?? throw new InvalidOperationException("Tag not found.");
        if (tag.Scope == TagScope.Personal && tag.UserId == userId)
        {
            unitOfWork.Repository<Tag>().Remove(tag);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return;
        }

        await EnsureCanManageScopeAsync(userId, tag.Scope, tag.TenantId, tag.WorkspaceId, cancellationToken);

        unitOfWork.Repository<Tag>().Remove(tag);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureCanManageScopeAsync(Guid userId, TagScope scope, Guid? tenantId, Guid? workspaceId, CancellationToken cancellationToken)
    {
        if (scope == TagScope.Personal)
        {
            return;
        }

        if (scope == TagScope.System)
        {
            throw new UnauthorizedAccessException("System tags are managed outside this project.");
        }

        if (scope == TagScope.Workspace)
        {
            var workspace = workspaceId is { } id
                ? await unitOfWork.Repository<Workspace>().FindAsync(id, cancellationToken)
                : null;
            tenantId = workspace?.TenantId ?? tenantId;
        }

        if (tenantId is not { } targetTenantId)
        {
            throw new InvalidOperationException("Tenant or workspace scoped tags require a tenant context.");
        }

        await permissionService.EnsureTenantAsync(userId, targetTenantId, ApplicationPermission.TagManage, cancellationToken);
    }
}
