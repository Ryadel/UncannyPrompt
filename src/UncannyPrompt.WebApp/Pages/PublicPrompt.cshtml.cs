using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using UncannyPrompt.Application;

namespace UncannyPrompt.WebApp.Pages;

[AllowAnonymous]
public sealed class PublicPromptModel(ISharingService sharingService) : PageModel
{
    public PublicPromptDto? Prompt { get; private set; }

    public async Task OnGetAsync(string token, CancellationToken cancellationToken)
    {
        Prompt = await sharingService.GetPublicPromptAsync(token, cancellationToken);
    }
}
