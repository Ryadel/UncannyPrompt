using UncannyPrompt.Domain;

namespace UncannyPrompt.Application;

public interface IAuditService
{
    Task RecordAsync(Guid? actorUserId, string eventType, string? entityType = null, Guid? entityId = null, Guid? tenantId = null, object? data = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AuditEventDto>> ListAsync(Guid userId, Guid? tenantId = null, CancellationToken cancellationToken = default);
}
