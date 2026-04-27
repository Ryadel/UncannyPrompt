using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Localization;
using UncannyPrompt.Application;
using UncannyPrompt.WebApp.Security;

namespace UncannyPrompt.WebApp.Pages.Folders;

[Authorize]
public sealed class EditModel(
    ICurrentUserContext currentUser,
    IAppScopeService appScopeService,
    IFolderService folderService,
    IStringLocalizerFactory localizerFactory) : PageModel
{
    private readonly IStringLocalizer localizer = localizerFactory.Create("Pages.Folders.Edit", typeof(EditModel).Assembly.GetName().Name!);

    [BindProperty(SupportsGet = true)]
    public Guid? Id { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid ProjectId { get; set; }

    [BindProperty]
    public Guid? ParentFolderId { get; set; }

    [BindProperty]
    public string Name { get; set; } = string.Empty;

    public IEnumerable<SelectListItem> ProjectOptions { get; private set; } = [];
    public IEnumerable<SelectListItem> ParentFolderOptions { get; private set; } = [];
    public bool IsCreateMode => Id is null;
    public bool MustCreateProjectFirst => IsCreateMode && !ProjectOptions.Any();
    public string Title => localizer[IsCreateMode ? "TitleCreate" : "TitleEdit"].Value;
    public string? ErrorMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var userId = RequireUser();
        await LoadOptionsAsync(userId, cancellationToken);

        if (Id is null)
        {
            ProjectId = ProjectId == Guid.Empty
                ? ProjectOptions.FirstOrDefault()?.Value is { } value ? Guid.Parse(value) : Guid.Empty
                : ProjectId;
            await LoadParentFolderOptionsAsync(userId, cancellationToken);
            return Page();
        }

        var folder = (await folderService.ListAsync(userId, cancellationToken: cancellationToken))
            .FirstOrDefault(x => x.Id == Id.Value);
        if (folder is null)
        {
            return RedirectToPage("/Projects");
        }

        ProjectId = folder.ProjectId;
        ParentFolderId = folder.ParentFolderId;
        Name = folder.Name;
        await LoadParentFolderOptionsAsync(userId, cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var userId = RequireUser();
        await LoadOptionsAsync(userId, cancellationToken);
        await LoadParentFolderOptionsAsync(userId, cancellationToken);

        if (MustCreateProjectFirst || ProjectId == Guid.Empty)
        {
            ErrorMessage = localizer["ErrorProjectRequired"].Value;
            return Page();
        }

        if (string.IsNullOrWhiteSpace(Name))
        {
            ErrorMessage = localizer["ErrorNameRequired"].Value;
            return Page();
        }

        if (Id is Guid folderId)
        {
            await folderService.UpdateAsync(userId, folderId, Name, ParentFolderId, cancellationToken);
        }
        else
        {
            await folderService.CreateAsync(userId, ProjectId, ParentFolderId, Name, cancellationToken);
        }

        return RedirectToPage("/Projects");
    }

    private async Task LoadOptionsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var scopeState = await appScopeService.GetStateAsync(User, userId, cancellationToken);
        ProjectOptions = scopeState.Projects
            .Select(x => new SelectListItem(x.Name, x.Id.ToString()))
            .ToList();
    }

    private async Task LoadParentFolderOptionsAsync(Guid userId, CancellationToken cancellationToken)
    {
        ParentFolderOptions = ProjectId == Guid.Empty
            ? []
            : (await folderService.ListAsync(userId, ProjectId, cancellationToken))
                .Where(x => x.Id != Id)
                .Select(x => new SelectListItem(x.Name, x.Id.ToString()))
                .ToList();
    }

    private Guid RequireUser() => currentUser.UserId ?? throw new UnauthorizedAccessException();
}
