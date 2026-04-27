using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UncannyPrompt.Shared;
using UncannyPrompt.WebApp.Security;

namespace UncannyPrompt.WebApp.Controllers;

[ApiController]
[Route("api/scope")]
[Authorize(AuthenticationSchemes = AppConstants.CookieScheme)]
public sealed class ScopeController(IAppScopeService appScopeService) : UncannyControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Set([FromBody] ScopeUpdateRequest request, CancellationToken cancellationToken)
    {
        if (request.TenantId is not Guid tenantId)
        {
            return BadRequest();
        }

        var state = await appScopeService.SetStateAsync(User, CurrentUserId, tenantId, request.WorkspaceId, request.ProjectId, cancellationToken);
        return state is null ? NotFound() : Ok(state);
    }
}

public sealed record ScopeUpdateRequest(Guid? TenantId, Guid? WorkspaceId, Guid? ProjectId);
