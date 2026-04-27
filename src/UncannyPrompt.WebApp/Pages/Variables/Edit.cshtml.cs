using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Localization;
using UncannyPrompt.Application;
using UncannyPrompt.Domain;
using UncannyPrompt.WebApp.Security;

namespace UncannyPrompt.WebApp.Pages.Variables;

[Authorize]
public sealed class EditModel(
    ICurrentUserContext currentUser,
    IAppScopeService appScopeService,
    IVariableService variableService,
    IStringLocalizerFactory localizerFactory) : PageModel
{
    private readonly IStringLocalizer localizer = localizerFactory.Create("Pages.Variables.Edit", typeof(EditModel).Assembly.GetName().Name!);

    [BindProperty(SupportsGet = true)]
    public Guid? Id { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid? ProjectId { get; set; }

    [BindProperty]
    public string Name { get; set; } = string.Empty;

    [BindProperty]
    public string? Description { get; set; }

    [BindProperty]
    public string? DefaultValue { get; set; }

    [BindProperty]
    public bool IsRequired { get; set; }

    [BindProperty]
    public bool IsSecret { get; set; }

    [BindProperty]
    public VariableScope Scope { get; set; } = VariableScope.Project;

    [BindProperty]
    public string? Value { get; set; }

    public IEnumerable<SelectListItem> ProjectOptions { get; private set; } = [];
    public bool IsCreateMode => Id is null;
    public bool MustCreateProjectFirst => IsCreateMode && !ProjectOptions.Any();
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
        ProjectId ??= scopeState.CurrentProjectId
            ?? (ProjectOptions.FirstOrDefault()?.Value is { } value ? Guid.Parse(value) : null);

        if (Id is Guid id)
        {
            var variable = (await variableService.ListAsync(userId, ProjectId, cancellationToken))
                .FirstOrDefault(x => x.Id == id);
            if (variable is null)
            {
                return RedirectToPage("/Variables", new { projectId = ProjectId });
            }

            Name = variable.Name;
            Description = variable.Description;
            DefaultValue = variable.DefaultValue;
            IsRequired = variable.IsRequired;
            IsSecret = variable.IsSecret;
            Scope = variable.Scope;
            ProjectId = variable.ProjectId ?? ProjectId;
            Value = variable.IsSecret ? null : variable.Value;
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var userId = RequireUser();
        var scopeState = await appScopeService.GetStateAsync(User, userId, cancellationToken);
        LoadProjectOptions(scopeState);

        if (MustCreateProjectFirst || (Scope == VariableScope.Project && ProjectId is null))
        {
            ErrorMessage = localizer["ErrorProjectRequired"].Value;
            return Page();
        }

        if (string.IsNullOrWhiteSpace(Name))
        {
            ErrorMessage = localizer["ErrorNameRequired"].Value;
            return Page();
        }

        var projectId = Scope == VariableScope.Project ? ProjectId : null;
        await variableService.UpsertAsync(
            userId,
            new VariableUpsertRequest(
                Id,
                Name,
                Description,
                DefaultValue,
                IsRequired,
                IsSecret,
                Scope,
                projectId,
                Value),
            cancellationToken);

        return RedirectToPage("/Variables", new { projectId = projectId ?? ProjectId });
    }

    private void LoadProjectOptions(AppScopeState scopeState)
    {
        ProjectOptions = scopeState.Projects
            .Select(x => new SelectListItem(x.Name, x.Id.ToString()))
            .ToList();
    }

    private Guid RequireUser() => currentUser.UserId ?? throw new UnauthorizedAccessException();
}
