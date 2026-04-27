namespace UncannyPrompt.Domain;

public sealed class PromptNote : Entity
{
    public Guid PromptId { get; set; }
    public Prompt? Prompt { get; set; }
    public Guid AuthorUserId { get; set; }
    public User? Author { get; set; }
    public string Body { get; set; } = string.Empty;
}
