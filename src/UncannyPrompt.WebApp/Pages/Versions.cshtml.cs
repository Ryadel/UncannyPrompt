using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using UncannyPrompt.Application;

namespace UncannyPrompt.WebApp.Pages;

[Authorize]
public sealed class VersionsModel(
    ICurrentUserContext currentUser,
    IPromptService promptService,
    IPromptVersionService versionService) : PageModel
{
    public IReadOnlyList<PromptDto> Prompts { get; private set; } = [];
    public IReadOnlyList<PromptVersionDto> Versions { get; private set; } = [];
    public string? Diff { get; private set; }

    [BindProperty(SupportsGet = true)]
    public Guid? PromptId { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid? LeftVersionId { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid? RightVersionId { get; set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var userId = RequireUser();
        Prompts = await promptService.ListAsync(userId, new PromptSearchRequest(PageSize: 100), cancellationToken);
        PromptId ??= Prompts.FirstOrDefault()?.Id;
        if (PromptId is { } promptId)
        {
            Versions = await versionService.ListAsync(userId, promptId, cancellationToken);
        }

        if (LeftVersionId is { } left && RightVersionId is { } right)
        {
            Diff = await versionService.DiffAsync(userId, left, right, cancellationToken);
        }
    }

    public async Task<IActionResult> OnPostRestoreAsync(Guid promptId, Guid versionId, string? changelog, CancellationToken cancellationToken)
    {
        await versionService.RestoreAsync(RequireUser(), promptId, versionId, changelog, cancellationToken);
        return RedirectToPage(new { promptId });
    }

    private Guid RequireUser() => currentUser.UserId ?? throw new UnauthorizedAccessException();
}
