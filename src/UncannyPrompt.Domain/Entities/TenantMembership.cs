namespace UncannyPrompt.Domain;

public sealed class TenantMembership : Entity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public TenantRole Role { get; set; } = TenantRole.Viewer;
    public bool IsActive { get; set; } = true;
    public bool CanGrant { get; set; }
    public Guid? GrantedByUserId { get; set; }
    public User? GrantedByUser { get; set; }
    public DateTimeOffset? GrantedAt { get; set; }
}
