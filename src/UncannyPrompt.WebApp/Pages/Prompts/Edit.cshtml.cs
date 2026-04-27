using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Localization;
using UncannyPrompt.Application;
using UncannyPrompt.Domain;
using UncannyPrompt.WebApp.Security;

namespace UncannyPrompt.WebApp.Pages.Prompts;

[Authorize]
public sealed class EditModel(
    ICurrentUserContext currentUser,
    IAppScopeService appScopeService,
    IPromptService promptService,
    IFolderService folderService,
    IStringLocalizerFactory localizerFactory) : PageModel
{
    private readonly IStringLocalizer localizer = localizerFactory.Create("Pages.Prompts.Edit", typeof(EditModel).Assembly.GetName().Name!);

    [BindProperty(SupportsGet = true)]
    public Guid? Id { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid ProjectId { get; set; }

    [BindProperty]
    public Guid? FolderId { get; set; }

    [BindProperty]
    public string Title { get; set; } = string.Empty;

    [BindProperty]
    public string? Summary { get; set; }

    [BindProperty]
    public string PromptContent { get; set; } = string.Empty;

    [BindProperty]
    public string? TagsText { get; set; }

    [BindProperty]
    public PromptStatus Status { get; set; } = PromptStatus.Draft;

    [BindProperty]
    public string? Changelog { get; set; }

    public IEnumerable<SelectListItem> ProjectOptions { get; private set; } = [];
    public IEnumerable<SelectListItem> FolderOptions { get; private set; } = [];
    public bool IsCreateMode => Id is null;
    public bool MustCreateProjectFirst => IsCreateMode && !ProjectOptions.Any();
    public string PageTitle => localizer[IsCreateMode ? "TitleCreate" : "TitleEdit"].Value;
    public string? ErrorMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var userId = RequireUser();
        var scopeState = await appScopeService.GetStateAsync(User, userId, cancellationToken);
        if (scopeState.CurrentWorkspaceId is null)
        {
            return RedirectToPage("/Workspaces/Edit");
        }

        LoadProjectOptions(scopeState);

        if (Id is null)
        {
            ProjectId = ProjectId == Guid.Empty
                ? scopeState.CurrentProjectId ?? (ProjectOptions.FirstOrDefault()?.Value is { } value ? Guid.Parse(value) : Guid.Empty)
                : ProjectId;
            await LoadFolderOptionsAsync(userId, cancellationToken);
            return Page();
        }

        var prompt = await promptService.GetAsync(userId, Id.Value, cancellationToken);
        if (prompt is null)
        {
            return RedirectToPage("/Prompts");
        }

        ProjectId = prompt.ProjectId;
        FolderId = prompt.FolderId;
        Title = prompt.Title;
        Summary = prompt.Summary;
        PromptContent = prompt.Content;
        TagsText = string.Join(", ", prompt.Tags);
        Status = prompt.Status;
        await LoadFolderOptionsAsync(userId, cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var userId = RequireUser();
        var scopeState = await appScopeService.GetStateAsync(User, userId, cancellationToken);
        LoadProjectOptions(scopeState);
        await LoadFolderOptionsAsync(userId, cancellationToken);

        if (MustCreateProjectFirst || ProjectId == Guid.Empty)
        {
            ErrorMessage = localizer["ErrorProjectRequired"].Value;
            return Page();
        }

        if (string.IsNullOrWhiteSpace(Title) || string.IsNullOrWhiteSpace(PromptContent))
        {
            ErrorMessage = localizer["ErrorTitleAndContentRequired"].Value;
            return Page();
        }

        var request = new PromptUpsertRequest(
            ProjectId,
            FolderId,
            Title,
            Summary,
            PromptContent,
            null,
            Status,
            SplitTags(TagsText),
            string.IsNullOrWhiteSpace(Changelog) ? null : "manual-save",
            Changelog);

        if (Id is Guid promptId)
        {
            await promptService.UpdateAsync(userId, promptId, request, cancellationToken);
        }
        else
        {
            await promptService.CreateAsync(userId, request, cancellationToken);
        }

        return RedirectToPage("/Prompts");
    }

    private void LoadProjectOptions(AppScopeState scopeState)
    {
        ProjectOptions = scopeState.Projects
            .Select(x => new SelectListItem(x.Name, x.Id.ToString()))
            .ToList();
    }

    private async Task LoadFolderOptionsAsync(Guid userId, CancellationToken cancellationToken)
    {
        FolderOptions = ProjectId == Guid.Empty
            ? []
            : (await folderService.ListAsync(userId, ProjectId, cancellationToken))
                .Select(x => new SelectListItem(x.Name, x.Id.ToString()))
                .ToList();
    }

    private static IReadOnlyList<string> SplitTags(string? tagsText) =>
        string.IsNullOrWhiteSpace(tagsText)
            ? []
            : tagsText.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private Guid RequireUser() => currentUser.UserId ?? throw new UnauthorizedAccessException();
}
