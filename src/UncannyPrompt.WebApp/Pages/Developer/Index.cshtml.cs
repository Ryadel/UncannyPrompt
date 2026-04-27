using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace UncannyPrompt.WebApp.Pages.Developer;

[Authorize]
public sealed class IndexModel : PageModel
{
    public IActionResult OnGet() => RedirectToPage("/Developer/Api/Index");
}
