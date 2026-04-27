using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UncannyPrompt.Application;
using UncannyPrompt.Shared;

namespace UncannyPrompt.WebApp.Controllers;

[ApiController]
[Route("api/tags")]
[Authorize(AuthenticationSchemes = $"{AppConstants.CookieScheme},{AppConstants.ApiKeyScheme}")]
public sealed class TagsController(ITagService tagService) : UncannyControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<TagDto>> List(CancellationToken cancellationToken) =>
        tagService.ListAsync(CurrentUserId, cancellationToken);

    [HttpPost]
    public Task<TagDto> Create([FromBody] TagCreateRequest request, CancellationToken cancellationToken) =>
        tagService.CreateAsync(CurrentUserId, request.Name, cancellationToken: cancellationToken);

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await tagService.DeleteAsync(CurrentUserId, id, cancellationToken);
        return NoContent();
    }
}
