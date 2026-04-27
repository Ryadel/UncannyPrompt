using UncannyPrompt.Domain;

namespace UncannyPrompt.Application;

public sealed record PromptResolveRequest(Dictionary<string, string?> Values);
