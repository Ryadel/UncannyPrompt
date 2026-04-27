namespace UncannyPrompt.Domain;

public sealed class PublicShareLink : Entity, ISoftDeletable
{
    public Guid PromptId { get; set; }
    public Prompt? Prompt { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public string TokenLookupHash { get; set; } = string.Empty;
    public DateTimeOffset? ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}
