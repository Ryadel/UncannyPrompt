using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Localization;
using UncannyPrompt.Application;
using UncannyPrompt.Shared;

namespace UncannyPrompt.WebApp.Areas.Admin.Pages.Settings;

[Authorize(Policy = AppConstants.AdminOnlyPolicy)]
public sealed class IndexModel(
    ICurrentUserContext currentUser,
    IAdminService adminService,
    IAppSettingsService appSettingsService,
    IStringLocalizerFactory localizerFactory) : PageModel
{
    private readonly IStringLocalizer localizer = localizerFactory.Create("Areas.Admin.Pages.Settings.Index", typeof(IndexModel).Assembly.GetName().Name!);

    [BindProperty(SupportsGet = true)]
    public Guid? TenantId { get; set; }

    [BindProperty]
    public bool PublicSharingEnabled { get; set; }

    [BindProperty]
    public bool PromptCopyTrackingEnabled { get; set; }

    [BindProperty]
    public bool OnboardingEnabled { get; set; }

    [BindProperty]
    public int PromptVersionHistoryLimit { get; set; }

    [BindProperty]
    public int MaxPromptsPerProject { get; set; }

    [TempData]
    public string? SuccessMessage { get; set; }

    public string ScopeName { get; private set; } = string.Empty;
    public DateTimeOffset? UpdatedAt { get; private set; }
    public IEnumerable<SelectListItem> TenantOptions { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (PromptVersionHistoryLimit is < 1 or > 200)
        {
            ModelState.AddModelError(nameof(PromptVersionHistoryLimit), localizer["HistoryLimitValidation"].Value);
        }

        if (MaxPromptsPerProject is < 0 or > 10000)
        {
            ModelState.AddModelError(nameof(MaxPromptsPerProject), localizer["MaxPromptsValidation"].Value);
        }

        if (!ModelState.IsValid)
        {
            await LoadTenantsAsync(cancellationToken);
            return Page();
        }

        var saved = await appSettingsService.SaveAsync(
            RequireUser(),
            new AppSettingsSaveRequest(
                TenantId,
                PublicSharingEnabled,
                PromptCopyTrackingEnabled,
                OnboardingEnabled,
                PromptVersionHistoryLimit,
                MaxPromptsPerProject),
            cancellationToken);

        var savedScopeName = TenantId is null ? localizer["GlobalOption"].Value : saved.ScopeName;
        SuccessMessage = localizer["SavedMessage", savedScopeName].Value;
        return RedirectToPage("/Settings/Index", new { area = "Admin", tenantId = TenantId });
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        await LoadTenantsAsync(cancellationToken);
        var settings = await appSettingsService.GetAsync(TenantId, cancellationToken);
        ScopeName = TenantId is null ? localizer["GlobalOption"].Value : settings.ScopeName;
        UpdatedAt = settings.UpdatedAt;
        PublicSharingEnabled = settings.PublicSharingEnabled;
        PromptCopyTrackingEnabled = settings.PromptCopyTrackingEnabled;
        OnboardingEnabled = settings.OnboardingEnabled;
        PromptVersionHistoryLimit = settings.PromptVersionHistoryLimit;
        MaxPromptsPerProject = settings.MaxPromptsPerProject;
    }

    private async Task LoadTenantsAsync(CancellationToken cancellationToken)
    {
        var tenants = await adminService.ListTenantsAsync(cancellationToken);
        TenantOptions = new[] { new SelectListItem(localizer["GlobalOption"].Value, string.Empty) }
            .Concat(tenants.Select(x => new SelectListItem(x.Name, x.Id.ToString())))
            .ToList();
    }

    private Guid RequireUser() => currentUser.UserId ?? throw new UnauthorizedAccessException();
}
