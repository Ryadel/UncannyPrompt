using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UncannyPrompt.Application;
using UncannyPrompt.Shared;

namespace UncannyPrompt.WebApp.Controllers;

[ApiController]
[Route("api/workspaces")]
[Authorize(AuthenticationSchemes = $"{AppConstants.CookieScheme},{AppConstants.ApiKeyScheme}")]
public sealed class WorkspacesController(IWorkspaceService workspaceService, ITenantScopeService scopeService) : UncannyControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<WorkspaceDto>> List([FromQuery] Guid? tenantId, CancellationToken cancellationToken) =>
        workspaceService.ListAsync(CurrentUserId, tenantId, cancellationToken);

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<WorkspaceDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var workspace = await workspaceService.GetAsync(CurrentUserId, id, cancellationToken);
        return workspace is null ? NotFound() : workspace;
    }

    [HttpPost]
    public async Task<ActionResult<WorkspaceDto>> Create([FromBody] WorkspaceCreateRequest request, CancellationToken cancellationToken)
    {
        var tenantId = request.TenantId ?? (await scopeService.GetDefaultScopeAsync(CurrentUserId, cancellationToken)).TenantId;
        return await workspaceService.CreateAsync(CurrentUserId, tenantId, request.Name, request.Description, cancellationToken);
    }

    [HttpPut("{id:guid}")]
    public Task<WorkspaceDto> Update(Guid id, [FromBody] WorkspaceCreateRequest request, CancellationToken cancellationToken) =>
        workspaceService.UpdateAsync(CurrentUserId, id, request.Name, request.Description, cancellationToken);

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await workspaceService.DeleteAsync(CurrentUserId, id, cancellationToken);
        return NoContent();
    }
}
