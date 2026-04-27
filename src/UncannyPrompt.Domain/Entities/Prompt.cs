namespace UncannyPrompt.Domain;

public sealed class Prompt : Entity, ISoftDeletable
{
    public Guid ProjectId { get; set; }
    public Project? Project { get; set; }
    public Guid? FolderId { get; set; }
    public Folder? Folder { get; set; }
    public Guid AuthorUserId { get; set; }
    public User? Author { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public PromptStatus Status { get; set; } = PromptStatus.Draft;
    public int CurrentVersionNumber { get; set; } = 1;
    public bool IsPinned { get; set; }
    public long CopyCount { get; set; }
    public DateTimeOffset? LastUsedAt { get; set; }
    public ICollection<PromptVersion> Versions { get; set; } = [];
    public ICollection<PromptTag> PromptTags { get; set; } = [];
    public ICollection<PromptNote> PromptNotes { get; set; } = [];
    public ICollection<Favorite> Favorites { get; set; } = [];
    public ICollection<ShareGrant> ShareGrants { get; set; } = [];
    public ICollection<PublicShareLink> PublicShareLinks { get; set; } = [];
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}
