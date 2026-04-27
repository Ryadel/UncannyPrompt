namespace UncannyPrompt.Domain;

public sealed class Project : Entity, ISoftDeletable
{
    public Guid WorkspaceId { get; set; }
    public Workspace? Workspace { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ICollection<Folder> Folders { get; set; } = [];
    public ICollection<Prompt> Prompts { get; set; } = [];
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}
