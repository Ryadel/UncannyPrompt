using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UncannyPrompt.Application;
using UncannyPrompt.Shared;

namespace UncannyPrompt.WebApp.Controllers;

[ApiController]
[Route("api/variables")]
[Authorize(AuthenticationSchemes = $"{AppConstants.CookieScheme},{AppConstants.ApiKeyScheme}")]
public sealed class VariablesController(IVariableService variableService) : UncannyControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<VariableDto>> List([FromQuery] Guid? projectId, CancellationToken cancellationToken) =>
        variableService.ListAsync(CurrentUserId, projectId, cancellationToken);

    [HttpPost]
    public Task<VariableDto> Create([FromBody] VariableUpsertRequest request, CancellationToken cancellationToken) =>
        variableService.UpsertAsync(CurrentUserId, request with { Id = null }, cancellationToken);

    [HttpPut("{id:guid}")]
    public Task<VariableDto> Update(Guid id, [FromBody] VariableUpsertRequest request, CancellationToken cancellationToken) =>
        variableService.UpsertAsync(CurrentUserId, request with { Id = id }, cancellationToken);

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await variableService.DeleteAsync(CurrentUserId, id, cancellationToken);
        return NoContent();
    }
}
