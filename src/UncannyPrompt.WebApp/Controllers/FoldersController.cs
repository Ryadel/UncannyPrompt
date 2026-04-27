using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UncannyPrompt.Application;
using UncannyPrompt.Shared;

namespace UncannyPrompt.WebApp.Controllers;

[ApiController]
[Route("api/folders")]
[Authorize(AuthenticationSchemes = $"{AppConstants.CookieScheme},{AppConstants.ApiKeyScheme}")]
public sealed class FoldersController(IFolderService folderService) : UncannyControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<FolderDto>> List([FromQuery] Guid? projectId, CancellationToken cancellationToken) =>
        folderService.ListAsync(CurrentUserId, projectId, cancellationToken);

    [HttpPost]
    public Task<FolderDto> Create([FromBody] FolderUpsertApiRequest request, CancellationToken cancellationToken) =>
        folderService.CreateAsync(CurrentUserId, request.ProjectId, request.ParentFolderId, request.Name, cancellationToken);

    [HttpPut("{id:guid}")]
    public Task<FolderDto> Update(Guid id, [FromBody] FolderUpsertApiRequest request, CancellationToken cancellationToken) =>
        folderService.UpdateAsync(CurrentUserId, id, request.Name, request.ParentFolderId, cancellationToken);

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await folderService.DeleteAsync(CurrentUserId, id, cancellationToken);
        return NoContent();
    }
}
