namespace UncannyPrompt.Domain;

public sealed class PromptTag
{
    public Guid PromptId { get; set; }
    public Prompt? Prompt { get; set; }
    public Guid TagId { get; set; }
    public Tag? Tag { get; set; }
}
