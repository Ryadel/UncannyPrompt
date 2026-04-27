using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UncannyPrompt.Application;
using UncannyPrompt.Shared;

namespace UncannyPrompt.WebApp.Controllers;

[ApiController]
[Route("api/projects")]
[Authorize(AuthenticationSchemes = $"{AppConstants.CookieScheme},{AppConstants.ApiKeyScheme}")]
public sealed class ProjectsController(IProjectService projectService, ITenantScopeService scopeService) : UncannyControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<ProjectDto>> List([FromQuery] Guid? workspaceId, CancellationToken cancellationToken) =>
        projectService.ListAsync(CurrentUserId, workspaceId, cancellationToken);

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProjectDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var project = await projectService.GetAsync(CurrentUserId, id, cancellationToken);
        return project is null ? NotFound() : project;
    }

    [HttpPost]
    public async Task<ActionResult<ProjectDto>> Create([FromBody] ProjectCreateRequest request, CancellationToken cancellationToken)
    {
        var workspaceId = request.WorkspaceId ?? (await scopeService.GetDefaultScopeAsync(CurrentUserId, cancellationToken)).WorkspaceId;
        return await projectService.CreateAsync(CurrentUserId, workspaceId, request.Name, request.Description, cancellationToken);
    }

    [HttpPut("{id:guid}")]
    public Task<ProjectDto> Update(Guid id, [FromBody] ProjectCreateRequest request, CancellationToken cancellationToken) =>
        projectService.UpdateAsync(CurrentUserId, id, request.Name, request.Description, cancellationToken);

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await projectService.DeleteAsync(CurrentUserId, id, cancellationToken);
        return NoContent();
    }
}
