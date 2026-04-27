using UncannyPrompt.Domain;

namespace UncannyPrompt.Application;

public interface ITenantService
{
    Task<IReadOnlyList<TenantDto>> ListAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<TenantDto> CreateCollaborativeAsync(Guid ownerUserId, string name, CancellationToken cancellationToken = default);
}
