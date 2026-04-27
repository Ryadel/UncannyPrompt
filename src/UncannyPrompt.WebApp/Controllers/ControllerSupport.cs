using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace UncannyPrompt.WebApp.Controllers;

public abstract class UncannyControllerBase : ControllerBase
{
    protected Guid CurrentUserId
    {
        get
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(value, out var id) ? id : throw new UnauthorizedAccessException("Missing user id.");
        }
    }
}
