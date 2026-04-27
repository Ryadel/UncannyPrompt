using UncannyPrompt.Domain;

namespace UncannyPrompt.Application;

public sealed record PromptResolutionResult(string RawContent, string ResolvedContent, IReadOnlyList<string> Placeholders, IReadOnlyList<string> MissingVariables);
