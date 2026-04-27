using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using UncannyPrompt.Domain;

namespace UncannyPrompt.Application;

internal sealed class SharingService(IUnitOfWork unitOfWork, IPermissionService permissionService, IPublicTokenHasher tokenHasher, IAuditService auditService) : ISharingService
{
    public async Task<IReadOnlyList<ShareGrantDto>> ListAsync(Guid userId, ShareTargetType? targetType = null, Guid? targetId = null, CancellationToken cancellationToken = default)
    {
        var shareableWorkspaceIds = AccessControlQueries.AccessibleWorkspaceIds(unitOfWork, userId, SharePermission.Edit);
        var shareableProjectIds = AccessControlQueries.AccessibleProjectIds(unitOfWork, userId, SharePermission.Edit);
        var shareableFolderIds = await AccessControlQueries.AccessibleFolderIdsAsync(unitOfWork, userId, SharePermission.Edit, cancellationToken);
        var shareablePromptIds = AccessControlQueries.AccessiblePromptIds(unitOfWork, userId, SharePermission.Edit);
        var folderIdsInShareableProjects = unitOfWork.Repository<Folder>().Query()
            .Where(x => !x.IsDeleted && shareableProjectIds.Contains(x.ProjectId))
            .Select(x => x.Id);
        var promptIdsInShareableProjects = unitOfWork.Repository<Prompt>().Query()
            .Where(x => !x.IsDeleted && shareableProjectIds.Contains(x.ProjectId))
            .Select(x => x.Id);

        var grants = await unitOfWork.Repository<ShareGrant>().Query()
            .Where(x => !x.IsDeleted &&
                        (targetType == null || x.TargetType == targetType) &&
                        (targetId == null || x.TargetId == targetId) &&
                        ((x.TargetType == ShareTargetType.Workspace && shareableWorkspaceIds.Contains(x.TargetId)) ||
                         (x.TargetType == ShareTargetType.Project && shareableProjectIds.Contains(x.TargetId)) ||
                         (x.TargetType == ShareTargetType.Folder && (shareableFolderIds.Contains(x.TargetId) || folderIdsInShareableProjects.Contains(x.TargetId))) ||
                         (x.TargetType == ShareTargetType.Prompt && (shareablePromptIds.Contains(x.TargetId) || promptIdsInShareableProjects.Contains(x.TargetId)))))
            .OrderByDescending(x => x.CreatedAt)
            .Take(200)
            .Select(x => x.ToDto())
            .ToListAsync(cancellationToken);
        return grants;
    }

    public async Task<ShareGrantDto> GrantAsync(Guid userId, ShareGrantRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureCanShareAsync(userId, request.TargetType, request.TargetId, cancellationToken);
        var grant = new ShareGrant
        {
            TargetType = request.TargetType,
            TargetId = request.TargetId,
            UserId = request.UserId,
            TenantId = request.TenantId,
            WorkspaceId = request.WorkspaceId,
            Permission = request.Permission
        };
        await unitOfWork.Repository<ShareGrant>().AddAsync(grant, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await auditService.RecordAsync(userId, "sharing.grant", "ShareGrant", grant.Id, data: request, cancellationToken: cancellationToken);
        return grant.ToDto();
    }

    public async Task DeleteGrantAsync(Guid userId, Guid grantId, CancellationToken cancellationToken = default)
    {
        var grant = await unitOfWork.Repository<ShareGrant>().FindAsync(grantId, cancellationToken) ?? throw new InvalidOperationException("Grant not found.");
        await EnsureCanShareAsync(userId, grant.TargetType, grant.TargetId, cancellationToken);
        grant.IsDeleted = true;
        grant.DeletedAt = DateTimeOffset.UtcNow;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await auditService.RecordAsync(userId, "sharing.revoke", "ShareGrant", grant.Id, cancellationToken: cancellationToken);
    }

    public async Task<PublicLinkDto> CreatePublicLinkAsync(Guid userId, Guid promptId, DateTimeOffset? expiresAt, CancellationToken cancellationToken = default)
    {
        var prompt = await unitOfWork.Repository<Prompt>().FindAsync(promptId, cancellationToken) ?? throw new InvalidOperationException("Prompt not found.");
        await permissionService.EnsurePromptAsync(userId, prompt.Id, ApplicationPermission.PromptPublish, cancellationToken);

        var token = tokenHasher.CreateToken(24);
        var lookupHash = tokenHasher.CreateLookupHash(token);
        var tokenHash = tokenHasher.HashToken(token);
        var link = new PublicShareLink { PromptId = promptId, TokenHash = tokenHash, TokenLookupHash = lookupHash, ExpiresAt = expiresAt };
        await unitOfWork.Repository<PublicShareLink>().AddAsync(link, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await auditService.RecordAsync(userId, "public_link.create", "PublicShareLink", link.Id, data: new { promptId }, cancellationToken: cancellationToken);
        return new PublicLinkDto(link.Id, promptId, token, expiresAt, link.CreatedAt);
    }

    public async Task DeletePublicLinkAsync(Guid userId, Guid publicLinkId, CancellationToken cancellationToken = default)
    {
        var link = await unitOfWork.Repository<PublicShareLink>().FindAsync(publicLinkId, cancellationToken) ?? throw new InvalidOperationException("Public link not found.");
        var prompt = await unitOfWork.Repository<Prompt>().FindAsync(link.PromptId, cancellationToken) ?? throw new InvalidOperationException("Prompt not found.");
        await permissionService.EnsurePromptAsync(userId, prompt.Id, ApplicationPermission.PromptPublish, cancellationToken);

        link.IsDeleted = true;
        link.RevokedAt = DateTimeOffset.UtcNow;
        link.DeletedAt = DateTimeOffset.UtcNow;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await auditService.RecordAsync(userId, "public_link.revoke", "PublicShareLink", link.Id, cancellationToken: cancellationToken);
    }

    public async Task<PublicPromptDto?> GetPublicPromptAsync(string token, CancellationToken cancellationToken = default)
    {
        var lookupHash = tokenHasher.CreateLookupHash(token);
        var link = await unitOfWork.Repository<PublicShareLink>().Query()
            .FirstOrDefaultAsync(x =>
                x.TokenLookupHash == lookupHash &&
                !x.IsDeleted &&
                x.RevokedAt == null &&
                (x.ExpiresAt == null || x.ExpiresAt > DateTimeOffset.UtcNow), cancellationToken);
        if (link is null || !tokenHasher.VerifyToken(token, link.TokenHash))
        {
            return null;
        }

        var prompt = await unitOfWork.Repository<Prompt>().Query().FirstOrDefaultAsync(x => x.Id == link.PromptId && !x.IsDeleted, cancellationToken);
        if (prompt is null)
        {
            return null;
        }

        var tags = prompt.ToDto(Guid.Empty, unitOfWork).Tags;
        await auditService.RecordAsync(null, "public_link.access", "PublicShareLink", link.Id, data: new { link.PromptId }, cancellationToken: cancellationToken);
        return new PublicPromptDto(prompt.Id, prompt.Title, prompt.Summary, prompt.Content, prompt.Notes, tags);
    }

    private async Task EnsureCanShareAsync(Guid userId, ShareTargetType targetType, Guid targetId, CancellationToken cancellationToken)
    {
        var canShare = targetType switch
        {
            ShareTargetType.Workspace => await permissionService.CanAccessWorkspaceAsync(userId, targetId, ApplicationPermission.PromptShare, cancellationToken),
            ShareTargetType.Project => await permissionService.CanAccessProjectAsync(userId, targetId, ApplicationPermission.PromptShare, cancellationToken),
            ShareTargetType.Folder => await permissionService.CanAccessFolderAsync(userId, targetId, ApplicationPermission.PromptShare, cancellationToken),
            ShareTargetType.Prompt => await permissionService.CanAccessPromptAsync(userId, targetId, ApplicationPermission.PromptShare, cancellationToken),
            _ => false
        };

        if (!canShare)
        {
            throw new UnauthorizedAccessException("Sharing denied.");
        }
    }
}
