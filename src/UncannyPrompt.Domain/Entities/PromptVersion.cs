namespace UncannyPrompt.Domain;

public sealed class PromptVersion : Entity
{
    public Guid PromptId { get; set; }
    public Prompt? Prompt { get; set; }
    public Guid AuthorUserId { get; set; }
    public User? Author { get; set; }
    public int VersionNumber { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? Label { get; set; }
    public string? Changelog { get; set; }
    public string? Notes { get; set; }
    public ICollection<VersionLabel> Labels { get; set; } = [];
}
