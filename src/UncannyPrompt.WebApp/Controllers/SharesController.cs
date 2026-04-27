using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UncannyPrompt.Application;
using UncannyPrompt.Domain;
using UncannyPrompt.Shared;

namespace UncannyPrompt.WebApp.Controllers;

[ApiController]
[Route("api/shares")]
[Authorize(AuthenticationSchemes = $"{AppConstants.CookieScheme},{AppConstants.ApiKeyScheme}")]
public sealed class SharesController(ISharingService sharingService) : UncannyControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<ShareGrantDto>> List([FromQuery] ShareTargetType? targetType, [FromQuery] Guid? targetId, CancellationToken cancellationToken) =>
        sharingService.ListAsync(CurrentUserId, targetType, targetId, cancellationToken);

    [HttpPost]
    public Task<ShareGrantDto> Grant([FromBody] ShareGrantRequest request, CancellationToken cancellationToken) =>
        sharingService.GrantAsync(CurrentUserId, request, cancellationToken);

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await sharingService.DeleteGrantAsync(CurrentUserId, id, cancellationToken);
        return NoContent();
    }
}
