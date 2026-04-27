using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using UncannyPrompt.Application;
using UncannyPrompt.Shared;

namespace UncannyPrompt.WebApp.Areas.Admin.Pages.Users;

[Authorize(Policy = AppConstants.AdminOnlyPolicy)]
public sealed class IndexModel(IAdminService adminService) : PageModel
{
    public IReadOnlyList<AdminUserDto> Users { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Users = await adminService.ListUsersAsync(cancellationToken);
    }
}
