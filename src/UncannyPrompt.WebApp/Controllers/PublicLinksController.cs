using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UncannyPrompt.Application;
using UncannyPrompt.Shared;

namespace UncannyPrompt.WebApp.Controllers;

[ApiController]
[Route("api/public-links")]
[Authorize(AuthenticationSchemes = $"{AppConstants.CookieScheme},{AppConstants.ApiKeyScheme}")]
public sealed class PublicLinksController(ISharingService sharingService) : UncannyControllerBase
{
    [HttpPost]
    public Task<PublicLinkDto> Create([FromBody] PublicLinkCreateRequest request, CancellationToken cancellationToken) =>
        sharingService.CreatePublicLinkAsync(CurrentUserId, request.PromptId, request.ExpiresAt, cancellationToken);

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await sharingService.DeletePublicLinkAsync(CurrentUserId, id, cancellationToken);
        return NoContent();
    }
}
