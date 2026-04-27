using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using UncannyPrompt.Application;
using UncannyPrompt.Shared;

namespace UncannyPrompt.WebApp.Areas.Admin.Pages;

[Authorize(Policy = AppConstants.AdminOnlyPolicy)]
public sealed class IndexModel(IAdminService adminService) : PageModel
{
    public AdminDashboardDto Dashboard { get; private set; } = new(0, 0, 0, 0, 0, 0, []);

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Dashboard = await adminService.GetDashboardAsync(cancellationToken);
    }
}
