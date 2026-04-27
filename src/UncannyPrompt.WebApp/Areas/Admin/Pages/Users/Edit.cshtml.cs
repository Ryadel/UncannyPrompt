using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Localization;
using UncannyPrompt.Application;
using UncannyPrompt.Domain;
using UncannyPrompt.Shared;

namespace UncannyPrompt.WebApp.Areas.Admin.Pages.Users;

[Authorize(Policy = AppConstants.AdminOnlyPolicy)]
public sealed class EditModel(
    ICurrentUserContext currentUser,
    IAdminService adminService,
    IStringLocalizerFactory localizerFactory) : PageModel
{
    private readonly IStringLocalizer localizer = localizerFactory.Create("Areas.Admin.Pages.Users.Edit", typeof(EditModel).Assembly.GetName().Name!);

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    [BindProperty]
    public UserRole Role { get; set; }

    [BindProperty]
    public UserStatus Status { get; set; }

    [TempData]
    public string? SuccessMessage { get; set; }

    public AdminUserDto? UserDetails { get; private set; }
    public IEnumerable<SelectListItem> RoleOptions { get; private set; } = [];
    public IEnumerable<SelectListItem> StatusOptions { get; private set; } = [];
    public string? ErrorMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadOptionsAsync();
        UserDetails = await adminService.GetUserAsync(Id, cancellationToken);
        if (UserDetails is null)
        {
            return RedirectToPage("/Users/Index", new { area = "Admin" });
        }

        Role = UserDetails.Role;
        Status = UserDetails.Status;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        await LoadOptionsAsync();
        UserDetails = await adminService.GetUserAsync(Id, cancellationToken);
        if (UserDetails is null)
        {
            return RedirectToPage("/Users/Index", new { area = "Admin" });
        }

        try
        {
            await adminService.SaveUserAsync(RequireUser(), new AdminUserSaveRequest(Id, Role, Status), cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            ErrorMessage = ex.Message == "At least one active admin is required."
                ? localizer["LastAdminError"].Value
                : localizer["SaveError"].Value;
            return Page();
        }

        SuccessMessage = localizer["SavedMessage"].Value;
        return RedirectToPage("/Users/Edit", new { area = "Admin", id = Id });
    }

    private Task LoadOptionsAsync()
    {
        RoleOptions = Enum.GetValues<UserRole>()
            .Select(x => new SelectListItem(x.ToString(), x.ToString()))
            .ToList();
        StatusOptions = Enum.GetValues<UserStatus>()
            .Select(x => new SelectListItem(x.ToString(), x.ToString()))
            .ToList();
        return Task.CompletedTask;
    }

    private Guid RequireUser() => currentUser.UserId ?? throw new UnauthorizedAccessException();
}
