namespace UncannyPrompt.Domain;

public sealed class Tag : Entity
{
    public Guid? TenantId { get; set; }
    public Tenant? Tenant { get; set; }
    public Guid? WorkspaceId { get; set; }
    public Workspace? Workspace { get; set; }
    public Guid? UserId { get; set; }
    public User? User { get; set; }
    public TagScope Scope { get; set; } = TagScope.Personal;
    public string Name { get; set; } = string.Empty;
    public ICollection<PromptTag> PromptTags { get; set; } = [];
}
