using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Localization;
using UncannyPrompt.Application;
using UncannyPrompt.WebApp.Security;

namespace UncannyPrompt.WebApp.Pages.Shares.PublicLinks;

[Authorize]
public sealed class EditModel(
    ICurrentUserContext currentUser,
    IAppScopeService appScopeService,
    IPromptService promptService,
    ISharingService sharingService,
    IStringLocalizerFactory localizerFactory) : PageModel
{
    private readonly IStringLocalizer localizer = localizerFactory.Create("Pages.Shares.PublicLinks.Edit", typeof(EditModel).Assembly.GetName().Name!);

    [BindProperty]
    public Guid PromptId { get; set; }

    [BindProperty]
    public DateTimeOffset? ExpiresAt { get; set; }

    [TempData]
    public string? PublicToken { get; set; }

    public IEnumerable<SelectListItem> PromptOptions { get; private set; } = [];
    public string? ErrorMessage { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadAsync(cancellationToken);
        PromptId = PromptOptions.FirstOrDefault()?.Value is { } value ? Guid.Parse(value) : Guid.Empty;
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        await LoadAsync(cancellationToken);
        if (PromptId == Guid.Empty)
        {
            ErrorMessage = localizer["ErrorPromptRequired"].Value;
            return Page();
        }

        var link = await sharingService.CreatePublicLinkAsync(RequireUser(), PromptId, ExpiresAt, cancellationToken);
        PublicToken = link.Token;
        return RedirectToPage("/Shares");
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        var userId = RequireUser();
        var scopeState = await appScopeService.GetStateAsync(User, userId, cancellationToken);
        var prompts = scopeState.CurrentProjectId is Guid projectId
            ? await promptService.ListAsync(userId, new PromptSearchRequest(ProjectId: projectId, PageSize: 100), cancellationToken)
            : [];
        PromptOptions = prompts.Select(x => new SelectListItem(x.Title, x.Id.ToString())).ToList();
    }

    private Guid RequireUser() => currentUser.UserId ?? throw new UnauthorizedAccessException();
}
