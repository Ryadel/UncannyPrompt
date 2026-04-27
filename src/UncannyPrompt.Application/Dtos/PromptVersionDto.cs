using UncannyPrompt.Domain;

namespace UncannyPrompt.Application;

public sealed record PromptVersionDto(Guid Id, Guid PromptId, int VersionNumber, string Content, string? Label, string? Changelog, string? Notes, DateTimeOffset CreatedAt);
