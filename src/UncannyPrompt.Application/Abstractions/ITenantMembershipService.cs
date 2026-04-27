using UncannyPrompt.Domain;

namespace UncannyPrompt.Application;

public interface ITenantMembershipService
{
    Task<IReadOnlyList<TenantMemberDto>> GetMembersAsync(Guid tenantId, CancellationToken cancellationToken = default);

    Task SaveAsync(
        Guid actorUserId,
        Guid tenantId,
        SaveTenantMembershipRequest request,
        bool isPlatformAdmin,
        CancellationToken cancellationToken = default);

    Task RevokeAsync(
        Guid actorUserId,
        Guid tenantId,
        Guid targetUserId,
        bool isPlatformAdmin,
        CancellationToken cancellationToken = default);
}
