using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using UncannyPrompt.Domain;

namespace UncannyPrompt.Application;

internal sealed class AuditService(IUnitOfWork unitOfWork, IPermissionService permissionService) : IAuditService
{
    public async Task RecordAsync(Guid? actorUserId, string eventType, string? entityType = null, Guid? entityId = null, Guid? tenantId = null, object? data = null, CancellationToken cancellationToken = default)
    {
        var auditEvent = new AuditEvent
        {
            ActorUserId = actorUserId,
            EventType = eventType,
            EntityType = entityType,
            EntityId = entityId,
            TenantId = tenantId,
            DataJson = data is null ? null : JsonSerializer.Serialize(data)
        };
        await unitOfWork.Repository<AuditEvent>().AddAsync(auditEvent, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AuditEventDto>> ListAsync(Guid userId, Guid? tenantId = null, CancellationToken cancellationToken = default)
    {
        var isPlatformAdmin = await unitOfWork.Repository<User>().Query()
            .AnyAsync(x => x.Id == userId && x.Status == UserStatus.Active && x.Role == UserRole.Admin, cancellationToken);

        if (tenantId is { } requestedTenantId)
        {
            await permissionService.EnsureTenantAsync(userId, requestedTenantId, ApplicationPermission.AuditView, cancellationToken);
        }

        var auditTenantIds = unitOfWork.Repository<TenantMembership>().Query()
            .Where(x => x.UserId == userId &&
                        x.IsActive &&
                        (x.Role == TenantRole.TenantOwner || x.Role == TenantRole.TenantManager))
            .Select(x => x.TenantId);

        var events = await unitOfWork.Repository<AuditEvent>().Query()
            .Where(x =>
                (tenantId == null || x.TenantId == tenantId) &&
                (isPlatformAdmin || x.ActorUserId == userId || (x.TenantId != null && auditTenantIds.Contains(x.TenantId.Value))))
            .OrderByDescending(x => x.CreatedAt)
            .Take(200)
            .Select(x => x.ToDto())
            .ToListAsync(cancellationToken);
        return events;
    }
}
