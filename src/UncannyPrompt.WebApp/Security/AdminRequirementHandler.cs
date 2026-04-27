using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using UncannyPrompt.Domain;
using UncannyPrompt.Infrastructure;

namespace UncannyPrompt.WebApp.Security;

public sealed class AdminRequirementHandler(UncannyPromptDbContext dbContext)
    : AuthorizationHandler<AdminRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, AdminRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            return;
        }

        var userIdValue = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdValue, out var userId))
        {
            return;
        }

        var isAdmin = await dbContext.Users
            .AsNoTracking()
            .AnyAsync(x => x.Id == userId && x.Status == UserStatus.Active && x.Role == UserRole.Admin);

        if (isAdmin)
        {
            context.Succeed(requirement);
        }
    }
}
