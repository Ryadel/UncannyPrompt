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
	IWorkspaceService workspaceService) : PageModel
{
	public AppScopeState ScopeState { get; private set; } = new(null, null, null, null, null, null, [], [], []);
	public IReadOnlyList<WorkspaceDto> Workspaces { get; private set; } = [];

	[BindProperty]
	public WorkspaceInput Workspace { get; set; } = new();

	public async Task OnGetAsync(CancellationToken cancellationToken)
	{
		await LoadAsync(cancellationToken);
	}

	public async Task<IActionResult> OnPostSaveAsync(CancellationToken cancellationToken)
	{
		var userId = RequireUser();
		if (Workspace.TenantId != Guid.Empty && !string.IsNullOrWhiteSpace(Workspace.Name))
		{
			if (Workspace.Id is Guid workspaceId)
			{
				await workspaceService.UpdateAsync(userId, workspaceId, Workspace.Name, Workspace.Description, cancellationToken);
			}
			else
			{
				await workspaceService.CreateAsync(userId, Workspace.TenantId, Workspace.Name, Workspace.Description, cancellationToken);
			}
		}

		return RedirectToPage();
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
		Workspace.TenantId = ScopeState.CurrentTenantId ?? ScopeState.Tenants.FirstOrDefault()?.Id ?? Guid.Empty;
	}

	private Guid RequireUser() => currentUser.UserId ?? throw new UnauthorizedAccessException();

	public sealed class WorkspaceInput
	{
		public Guid? Id { get; set; }
		public Guid TenantId { get; set; }
		public string Name { get; set; } = string.Empty;
		public string? Description { get; set; }
	}
}