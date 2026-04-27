using UncannyPrompt.Domain;

namespace UncannyPrompt.Application;

public sealed record VariableUpsertRequest(
    Guid? Id,
    string Name,
    string? Description,
    string? DefaultValue,
    bool IsRequired,
    bool IsSecret,
    VariableScope Scope,
    Guid? ProjectId,
    string? Value);
