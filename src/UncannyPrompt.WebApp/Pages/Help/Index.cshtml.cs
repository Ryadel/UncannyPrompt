using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace UncannyPrompt.WebApp.Pages.Help;

[AllowAnonymous]
public sealed class IndexModel : PageModel
{
    public void OnGet()
    {
    }
}
