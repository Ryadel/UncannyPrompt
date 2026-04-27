using Microsoft.AspNetCore.Mvc;
using UncannyPrompt.Application;

namespace UncannyPrompt.WebApp.Components;

public sealed class OnboardingOverlayViewComponent(
    ICurrentUserContext currentUser,
    IUserService userService) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        if (currentUser.UserId is not Guid userId)
        {
            return Content(string.Empty);
        }

        try
        {
            var user = await userService.GetAsync(userId, HttpContext.RequestAborted);
            if (user is null || user.OnboardingDismissedAt is not null)
            {
                return Content(string.Empty);
            }

            return View();
        }
        catch (OperationCanceledException)
        {
            // The client disconnected (e.g. navigated away or closed the tab) before
            // the request completed. HttpContext.RequestAborted is signalled by ASP.NET Core
            // in that situation, which causes EF Core's FindAsync to throw. This is expected
            // behaviour, so we swallow the exception and return an empty result.
            return Content(string.Empty);
        }
    }
}
