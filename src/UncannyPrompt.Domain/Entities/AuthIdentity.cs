namespace UncannyPrompt.Domain;

public sealed class AuthIdentity : Entity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string ProviderSubjectId { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? DisplayName { get; set; }
    public DateTimeOffset LastSeenAt { get; set; } = DateTimeOffset.UtcNow;
}
