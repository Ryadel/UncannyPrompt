using UncannyPrompt.Domain;

namespace UncannyPrompt.Application;

public sealed record PromptUpsertRequest(
    Guid ProjectId,
    Guid? FolderId,
    string Title,
    string? Summary,
    string Content,
    string? Notes,
    PromptStatus Status,
    IReadOnlyList<string>? Tags = null,
    string? VersionLabel = null,
    string? Changelog = null,
    string? VersionNotes = null);
