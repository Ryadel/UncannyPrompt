using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace UncannyPrompt.WebApp.Pages.Help;

[AllowAnonymous]
public sealed class OverviewModel : PageModel
{
    public void OnGet()
    {
    }
}
