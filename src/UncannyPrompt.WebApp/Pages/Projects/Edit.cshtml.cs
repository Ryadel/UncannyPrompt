using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Localization;
using UncannyPrompt.Application;
using UncannyPrompt.WebApp.Security;

namespace UncannyPrompt.WebApp.Pages.Projects;

[Authorize]
public sealed class EditModel(
    ICurrentUserContext currentUser,
    IAppScopeService appScopeService,
    IProjectService projectService,
    IStringLocalizerFactory localizerFactory) : PageModel
{
    private readonly IStringLocalizer localizer = localizerFactory.Create("Pages.Projects.Edit", typeof(EditModel).Assembly.GetName().Name!);

    [BindProperty(SupportsGet = true)]
    public Guid? Id { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid WorkspaceId { get; set; }

    [BindProperty]
    public string Name { get; set; } = string.Empty;

    [BindProperty]
    public string? Description { get; set; }

    public IEnumerable<SelectListItem> WorkspaceOptions { get; private set; } = [];
    public bool IsCreateMode => Id is null;
    public bool MustCreateWorkspaceFirst => IsCreateMode && !WorkspaceOptions.Any();
    public string Title => localizer[IsCreateMode ? "TitleCreate" : "TitleEdit"].Value;
    public string? ErrorMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var userId = RequireUser();
        await LoadWorkspaceOptionsAsync(userId, cancellationToken);

        if (Id is null)
        {
            WorkspaceId = WorkspaceId == Guid.Empty
                ? WorkspaceOptions.FirstOrDefault()?.Value is { } value ? Guid.Parse(value) : Guid.Empty
                : WorkspaceId;
            return Page();
        }

        var project = (await projectService.ListAsync(userId, cancellationToken: cancellationToken))
            .FirstOrDefault(x => x.Id == Id.Value);
        if (project is null)
        {
            return RedirectToPage("/Projects");
        }

        WorkspaceId = project.WorkspaceId;
        Name = project.Name;
        Description = project.Description;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var userId = RequireUser();
        await LoadWorkspaceOptionsAsync(userId, cancellationToken);

        if (MustCreateWorkspaceFirst || WorkspaceId == Guid.Empty)
        {
            ErrorMessage = localizer["ErrorWorkspaceRequired"].Value;
            return Page();
        }

        if (string.IsNullOrWhiteSpace(Name))
        {
            ErrorMessage = localizer["ErrorNameRequired"].Value;
            return Page();
        }

        if (Id is Guid projectId)
        {
            await projectService.UpdateAsync(userId, projectId, Name, Description, cancellationToken);
        }
        else
        {
            await projectService.CreateAsync(userId, WorkspaceId, Name, Description, cancellationToken);
        }

        return RedirectToPage("/Projects");
    }

    private async Task LoadWorkspaceOptionsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var scopeState = await appScopeService.GetStateAsync(User, userId, cancellationToken);
        WorkspaceOptions = scopeState.Workspaces
            .Select(x => new SelectListItem(x.Name, x.Id.ToString()))
            .ToList();
    }

    private Guid RequireUser() => currentUser.UserId ?? throw new UnauthorizedAccessException();
}
