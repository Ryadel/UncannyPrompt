using UncannyPrompt.Domain;

namespace UncannyPrompt.Application;

public sealed record AuditEventDto(Guid Id, string EventType, string? EntityType, Guid? EntityId, DateTimeOffset CreatedAt, string? DataJson);
