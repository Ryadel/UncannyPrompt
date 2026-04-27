using UncannyPrompt.Domain;

namespace UncannyPrompt.Application;

public sealed record PromptSearchRequest(
    string? Query = null,
    Guid? ProjectId = null,
    Guid? FolderId = null,
    Guid? AuthorUserId = null,
    PromptStatus? Status = null,
    string? Tag = null,
    bool FavoritesOnly = false,
    bool RecentOnly = false,
    string Sort = "updated",
    int Page = 1,
    int PageSize = 50);
