using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using UncannyPrompt.Application;
using UncannyPrompt.WebApp.Security;

namespace UncannyPrompt.WebApp.Pages.Developer.Api;

[Authorize]
public sealed class EditModel(IUserApiKeyService apiKeyService, ICurrentUserContext currentUser) : PageModel
{
    [BindProperty]
    public string Name { get; set; } = string.Empty;

    [BindProperty]
    public DateTime? ExpiresAtDate { get; set; }

    public ApiKeyCreateResultDto? CreatedApiKey { get; private set; }

    public string? SuccessMessageKey { get; private set; }

    public string? ErrorMessageKey { get; private set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var normalizedName = (Name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            ErrorMessageKey = "NameRequired";
            return Page();
        }

        DateTimeOffset? expiresAt = null;
        if (ExpiresAtDate is { } expiresDate)
        {
            if (expiresDate.Date <= DateTime.UtcNow.Date)
            {
                ErrorMessageKey = "ExpirationFuture";
                return Page();
            }

            var endOfDayUtc = DateTime.SpecifyKind(expiresDate.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
            expiresAt = new DateTimeOffset(endOfDayUtc);
        }

        try
        {
            CreatedApiKey = await apiKeyService.CreateAsync(
                RequireUser(),
                new ApiKeyCreateRequest(normalizedName, expiresAt),
                cancellationToken);
            SuccessMessageKey = "CreateSuccess";
            Name = string.Empty;
            ExpiresAtDate = null;
            return Page();
        }
        catch (ArgumentException)
        {
            ErrorMessageKey = "CreateFailed";
            return Page();
        }
    }

    private Guid RequireUser() => currentUser.UserId ?? throw new UnauthorizedAccessException();
}
