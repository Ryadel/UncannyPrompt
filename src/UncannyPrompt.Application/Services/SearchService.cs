using System.Text.Json;
using UncannyPrompt.Domain;

namespace UncannyPrompt.Application;

internal sealed class SearchService(IPromptService promptService) : ISearchService
{
    public Task<IReadOnlyList<PromptDto>> SearchAsync(Guid userId, PromptSearchRequest request, CancellationToken cancellationToken = default) =>
        promptService.ListAsync(userId, request, cancellationToken);
}
