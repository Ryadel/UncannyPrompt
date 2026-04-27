using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace UncannyPrompt.WebApp.Pages.Developer.Api;

[Authorize]
public sealed class DocsModel : PageModel
{
    public string BaseUrl => $"{Request.Scheme}://{Request.Host}{Request.PathBase}";

    public void OnGet()
    {
    }
}
