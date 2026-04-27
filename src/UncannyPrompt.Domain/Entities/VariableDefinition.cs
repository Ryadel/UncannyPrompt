namespace UncannyPrompt.Domain;

public sealed class VariableDefinition : Entity, ISoftDeletable
{
    public Guid? UserId { get; set; }
    public User? User { get; set; }
    public Guid? TenantId { get; set; }
    public Tenant? Tenant { get; set; }
    public Guid? WorkspaceId { get; set; }
    public Workspace? Workspace { get; set; }
    public Guid? ProjectId { get; set; }
    public Project? Project { get; set; }
    public VariableScope Scope { get; set; } = VariableScope.User;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? DefaultValue { get; set; }
    public bool IsRequired { get; set; }
    public bool IsSecret { get; set; }
    public ICollection<VariableValue> Values { get; set; } = [];
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}
