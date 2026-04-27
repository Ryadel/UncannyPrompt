using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UncannyPrompt.Application;
using UncannyPrompt.Domain;
using UncannyPrompt.Shared;

namespace UncannyPrompt.WebApp.Controllers;

[ApiController]
[Route("api/prompts")]
[Authorize(AuthenticationSchemes = $"{AppConstants.CookieScheme},{AppConstants.ApiKeyScheme}")]
public sealed class PromptsController(
    IPromptService promptService,
    IPromptVersionService versionService,
    IPromptResolutionService resolutionService) : UncannyControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<PromptDto>> List(
        [FromQuery] string? query,
        [FromQuery] Guid? projectId,
        [FromQuery] Guid? folderId,
        [FromQuery] Guid? authorUserId,
        [FromQuery] PromptStatus? status,
        [FromQuery] string? tag,
        [FromQuery] bool favoritesOnly = false,
        [FromQuery] bool recentOnly = false,
        [FromQuery] string sort = "updated",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default) =>
        promptService.ListAsync(CurrentUserId, new PromptSearchRequest(query, projectId, folderId, authorUserId, status, tag, favoritesOnly, recentOnly, sort, page, pageSize), cancellationToken);

    [HttpGet("by-tag/{tag}")]
    public Task<IReadOnlyList<PromptDto>> ListByTag(
        string tag,
        [FromQuery] Guid? projectId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default) =>
        promptService.ListAsync(CurrentUserId, new PromptSearchRequest(Query: null, ProjectId: projectId, FolderId: null, AuthorUserId: null, Status: null, Tag: tag, FavoritesOnly: false, RecentOnly: false, Sort: "updated", Page: page, PageSize: pageSize), cancellationToken);

    [HttpGet("by-folder/{folderId:guid}")]
    public Task<IReadOnlyList<PromptDto>> ListByFolder(
        Guid folderId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default) =>
        promptService.ListAsync(CurrentUserId, new PromptSearchRequest(Query: null, ProjectId: null, FolderId: folderId, AuthorUserId: null, Status: null, Tag: null, FavoritesOnly: false, RecentOnly: false, Sort: "updated", Page: page, PageSize: pageSize), cancellationToken);

    [HttpPost]
    public Task<PromptDto> Create([FromBody] PromptUpsertRequest request, CancellationToken cancellationToken) =>
        promptService.CreateAsync(CurrentUserId, request, cancellationToken);

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PromptDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var prompt = await promptService.GetAsync(CurrentUserId, id, cancellationToken);
        return prompt is null ? NotFound() : prompt;
    }

    [HttpPut("{id:guid}")]
    public Task<PromptDto> Update(Guid id, [FromBody] PromptUpsertRequest request, CancellationToken cancellationToken) =>
        promptService.UpdateAsync(CurrentUserId, id, request, cancellationToken);

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await promptService.DeleteAsync(CurrentUserId, id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/favorite")]
    public Task<PromptDto> ToggleFavorite(Guid id, CancellationToken cancellationToken) =>
        promptService.ToggleFavoriteAsync(CurrentUserId, id, cancellationToken);

    [HttpPost("{id:guid}/pinned")]
    public Task<PromptDto> TogglePinned(Guid id, CancellationToken cancellationToken) =>
        promptService.TogglePinnedAsync(CurrentUserId, id, cancellationToken);

    [HttpGet("{id:guid}/versions")]
    public Task<IReadOnlyList<PromptVersionDto>> Versions(Guid id, CancellationToken cancellationToken) =>
        versionService.ListAsync(CurrentUserId, id, cancellationToken);

    [HttpPost("{id:guid}/versions")]
    public Task<PromptVersionDto> CreateVersion(Guid id, [FromBody] VersionCreateRequest request, CancellationToken cancellationToken) =>
        versionService.CreateAsync(CurrentUserId, id, request, cancellationToken);

    [HttpPost("{id:guid}/restore")]
    public Task<PromptDto> Restore(Guid id, [FromBody] RestoreVersionRequest request, CancellationToken cancellationToken) =>
        versionService.RestoreAsync(CurrentUserId, id, request.VersionId, request.Changelog, cancellationToken);

    [HttpPost("{id:guid}/resolve")]
    public Task<PromptResolutionResult> Resolve(Guid id, [FromBody] PromptResolveRequest request, CancellationToken cancellationToken) =>
        resolutionService.ResolveAsync(CurrentUserId, id, request.Values, cancellationToken);

    [HttpPost("{id:guid}/copy-log")]
    public async Task<IActionResult> CopyLog(Guid id, [FromBody] CopyLogRequest request, CancellationToken cancellationToken)
    {
        await promptService.LogCopyAsync(CurrentUserId, id, request.Resolved, cancellationToken);
        return NoContent();
    }
}
