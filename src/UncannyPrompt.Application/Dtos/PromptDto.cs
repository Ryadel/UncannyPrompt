using UncannyPrompt.Domain;

namespace UncannyPrompt.Application;

public sealed record PromptDto(
    Guid Id,
    Guid ProjectId,
    Guid? FolderId,
    string Title,
    string? Summary,
    string Content,
    string? Notes,
    PromptStatus Status,
    int CurrentVersionNumber,
    bool IsFavorite,
    bool IsPinned,
    long CopyCount,
    DateTimeOffset? LastUsedAt,
    IReadOnlyList<string> Tags,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
