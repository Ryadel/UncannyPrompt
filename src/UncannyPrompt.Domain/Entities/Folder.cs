namespace UncannyPrompt.Domain;

public sealed class Folder : Entity, ISoftDeletable
{
    public Guid ProjectId { get; set; }
    public Project? Project { get; set; }
    public Guid? ParentFolderId { get; set; }
    public Folder? ParentFolder { get; set; }
    public string Name { get; set; } = string.Empty;
    public ICollection<Folder> Children { get; set; } = [];
    public ICollection<Prompt> Prompts { get; set; } = [];
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}
