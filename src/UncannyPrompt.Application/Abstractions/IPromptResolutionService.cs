using UncannyPrompt.Domain;

namespace UncannyPrompt.Application;

public interface IPromptResolutionService
{
    IReadOnlyList<string> ExtractPlaceholders(string content);
    Task<PromptResolutionResult> ResolveAsync(Guid userId, Guid promptId, IDictionary<string, string?> userValues, CancellationToken cancellationToken = default);
}
