namespace UncannyPrompt.Domain;

public sealed class VariableValue : Entity, ISoftDeletable
{
    public Guid VariableDefinitionId { get; set; }
    public VariableDefinition? VariableDefinition { get; set; }
    public Guid? UserId { get; set; }
    public Guid? ProjectId { get; set; }
    public Guid? PromptId { get; set; }
    public string? Value { get; set; }
    public bool IsEncrypted { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}
