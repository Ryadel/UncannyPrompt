namespace UncannyPrompt.Domain;

public sealed class AuditEvent : Entity
{
    public Guid? ActorUserId { get; set; }
    public User? ActorUser { get; set; }
    public Guid? TenantId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? EntityType { get; set; }
    public Guid? EntityId { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? DataJson { get; set; }
}
