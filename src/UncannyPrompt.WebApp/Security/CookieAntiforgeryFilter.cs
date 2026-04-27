using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc.Filters;
using UncannyPrompt.Shared;

namespace UncannyPrompt.WebApp.Security;

public sealed class CookieAntiforgeryFilter(IAntiforgery antiforgery) : IAsyncAuthorizationFilter
{
    private static readonly HashSet<string> SafeMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        HttpMethods.Get,
        HttpMethods.Head,
        HttpMethods.Options,
        HttpMethods.Trace
    };

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var request = context.HttpContext.Request;
        if (SafeMethods.Contains(request.Method))
        {
            return;
        }

        var identities = context.HttpContext.User.Identities;
        var hasCookieIdentity = identities.Any(x => x.IsAuthenticated && x.AuthenticationType == AppConstants.CookieScheme);
        var hasApiKeyIdentity = identities.Any(x => x.IsAuthenticated && x.AuthenticationType == AppConstants.ApiKeyScheme);
        if (hasCookieIdentity && !hasApiKeyIdentity)
        {
            await antiforgery.ValidateRequestAsync(context.HttpContext);
        }
    }
}
