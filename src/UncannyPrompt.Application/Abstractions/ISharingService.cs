using UncannyPrompt.Domain;

namespace UncannyPrompt.Application;

public interface ISharingService
{
    Task<IReadOnlyList<ShareGrantDto>> ListAsync(Guid userId, ShareTargetType? targetType = null, Guid? targetId = null, CancellationToken cancellationToken = default);
    Task<ShareGrantDto> GrantAsync(Guid userId, ShareGrantRequest request, CancellationToken cancellationToken = default);
    Task DeleteGrantAsync(Guid userId, Guid grantId, CancellationToken cancellationToken = default);
    Task<PublicLinkDto> CreatePublicLinkAsync(Guid userId, Guid promptId, DateTimeOffset? expiresAt, CancellationToken cancellationToken = default);
    Task DeletePublicLinkAsync(Guid userId, Guid publicLinkId, CancellationToken cancellationToken = default);
    Task<PublicPromptDto?> GetPublicPromptAsync(string token, CancellationToken cancellationToken = default);
}
