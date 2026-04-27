using UncannyPrompt.Domain;

namespace UncannyPrompt.Application;

public interface ISearchService
{
    Task<IReadOnlyList<PromptDto>> SearchAsync(Guid userId, PromptSearchRequest request, CancellationToken cancellationToken = default);
}
