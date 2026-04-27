namespace UncannyPrompt.Domain;

public sealed class VersionLabel : Entity
{
    public Guid PromptVersionId { get; set; }
    public PromptVersion? PromptVersion { get; set; }
    public string Name { get; set; } = string.Empty;
}
