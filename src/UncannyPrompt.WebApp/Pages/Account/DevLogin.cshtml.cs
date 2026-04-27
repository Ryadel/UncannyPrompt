using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace UncannyPrompt.WebApp.Pages.Account;

[AllowAnonymous]
public sealed class DevLoginModel(IWebHostEnvironment environment) : PageModel
{
    public string ReturnUrl { get; private set; } = "/";

    public IActionResult OnGet(string? returnUrl = null)
    {
        if (!environment.IsDevelopment())
        {
            return NotFound();
        }

        ReturnUrl = string.IsNullOrWhiteSpace(returnUrl) ? "/Prompts" : returnUrl;
        return Page();
    }
}
