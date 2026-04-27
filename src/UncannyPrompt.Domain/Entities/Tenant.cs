namespace UncannyPrompt.Domain;

public sealed class Tenant : Entity, ISoftDeletable
{
    public string Name { get; set; } = string.Empty;
    public TenantKind Kind { get; set; } = TenantKind.Personal;
    public Guid OwnerUserId { get; set; }
    public ICollection<TenantMembership> Memberships { get; set; } = [];
    public ICollection<Workspace> Workspaces { get; set; } = [];
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}
