using UncannyPrompt.Domain;

namespace UncannyPrompt.Application;

public sealed record PublicPromptDto(Guid Id, string Title, string? Summary, string Content, string? Notes, IReadOnlyList<string> Tags);
