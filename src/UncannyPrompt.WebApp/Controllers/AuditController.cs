using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UncannyPrompt.Application;
using UncannyPrompt.Shared;

namespace UncannyPrompt.WebApp.Controllers;

[ApiController]
[Route("api/audit")]
[Authorize(AuthenticationSchemes = $"{AppConstants.CookieScheme},{AppConstants.ApiKeyScheme}")]
public sealed class AuditController(IAuditService auditService) : UncannyControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<AuditEventDto>> List([FromQuery] Guid? tenantId, CancellationToken cancellationToken) =>
        auditService.ListAsync(CurrentUserId, tenantId, cancellationToken);
}
