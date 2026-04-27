using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using UncannyPrompt.Application;
using UncannyPrompt.WebApp.Security;

namespace UncannyPrompt.WebApp.Pages;

[Authorize]
public sealed class WorkspacesModel(
	ICurrentUserContext currentUser,
	IAppScopeService appScopeService,
	IWorkspaceService workspaceService,
	IProjectService projectService,
	IPromptService promptService) : PageModel
{
	public AppScopeState ScopeState { get; private set; } = new(null, null, null, null, null, null, [], [], []);
	public IReadOnlyList<WorkspaceDto> Workspaces { get; private set; } = [];
	public IReadOnlyDictionary<Guid, int> ProjectCountsByWorkspace { get; private set; } = new Dictionary<Guid, int>();
	public IReadOnlyDictionary<Guid, int> PromptCountsByWorkspace { get; private set; } = new Dictionary<Guid, int>();

	public async Task OnGetAsync(CancellationToken cancellationToken)
	{
		await LoadAsync(cancellationToken);
	}

	public async Task<IActionResult> OnPostDeleteAsync(Guid id, CancellationToken cancellationToken)
	{
		await workspaceService.DeleteAsync(RequireUser(), id, cancellationToken);
		return RedirectToPage();
	}

	private async Task LoadAsync(CancellationToken cancellationToken)
	{
		var userId = RequireUser();
		ScopeState = await appScopeService.GetStateAsync(User, userId, cancellationToken);
		Workspaces = ScopeState.CurrentTenantId is Guid tenantId
			? await workspaceService.ListAsync(userId, tenantId, cancellationToken)
			: [];

		var workspaceIds = Workspaces.Select(x => x.Id).ToList();
		ProjectCountsByWorkspace = await projectService.CountByWorkspaceAsync(userId, workspaceIds, cancellationToken);
		PromptCountsByWorkspace = await promptService.CountByWorkspaceAsync(userId, workspaceIds, cancellationToken);
	}

	private Guid RequireUser() => currentUser.UserId ?? throw new UnauthorizedAccessException();
}
