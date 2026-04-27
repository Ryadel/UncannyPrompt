namespace UncannyPrompt.Domain;

public sealed class Favorite : Entity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public Guid PromptId { get; set; }
    public Prompt? Prompt { get; set; }
}
