using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using UncannyPrompt.Application;
using UncannyPrompt.Shared;

namespace UncannyPrompt.WebApp.Areas.Admin.Pages.Tenants;

[Authorize(Policy = AppConstants.AdminOnlyPolicy)]
public sealed class IndexModel(IAdminService adminService) : PageModel
{
    public IReadOnlyList<AdminTenantDto> Tenants { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Tenants = await adminService.ListTenantsAsync(cancellationToken);
    }
}
