using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using UncannyPrompt.Application;

namespace UncannyPrompt.WebApp.Pages;

[Authorize]
public sealed class AuditModel(
    ICurrentUserContext currentUser,
    ITenantService tenantService,
    IAuditService auditService) : PageModel
{
    public IReadOnlyList<TenantDto> Tenants { get; private set; } = [];
    public IReadOnlyList<AuditEventDto> Events { get; private set; } = [];

    [BindProperty(SupportsGet = true)]
    public Guid? TenantId { get; set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedAccessException();
        Tenants = await tenantService.ListAsync(userId, cancellationToken);
        Events = await auditService.ListAsync(userId, TenantId, cancellationToken);
    }
}
