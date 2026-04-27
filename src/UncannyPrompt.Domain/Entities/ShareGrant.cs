namespace UncannyPrompt.Domain;

public sealed class ShareGrant : Entity, ISoftDeletable
{
    public ShareTargetType TargetType { get; set; }
    public Guid TargetId { get; set; }
    public Guid? UserId { get; set; }
    public Guid? TenantId { get; set; }
    public Guid? WorkspaceId { get; set; }
    public SharePermission Permission { get; set; } = SharePermission.View;
    public bool InheritsPermissions { get; set; } = true;
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}
