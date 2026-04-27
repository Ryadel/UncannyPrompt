using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using UncannyPrompt.Application;
using UncannyPrompt.Domain;
using UncannyPrompt.Shared;

namespace UncannyPrompt.WebApp.Security;

public sealed class ApiKeyAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IUnitOfWork unitOfWork,
    IApiKeyHasher apiKeyHasher)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var apiKey = Request.Headers["X-API-Key"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(apiKey) && Request.Headers.Authorization.FirstOrDefault() is { } authorization && authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            apiKey = authorization["Bearer ".Length..].Trim();
        }

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return AuthenticateResult.NoResult();
        }

        var matchedKey = unitOfWork.Repository<UserApiKey>().Query()
            .Where(x => !x.IsDeleted && (x.ExpiresAt == null || x.ExpiresAt > DateTimeOffset.UtcNow))
            .AsEnumerable()
            .FirstOrDefault(x => apiKeyHasher.Verify(apiKey, x.KeyHash));

        if (matchedKey is null)
        {
            return AuthenticateResult.Fail("Invalid API key.");
        }

        var user = unitOfWork.Repository<User>().Query().FirstOrDefault(x => x.Id == matchedKey.UserId && x.Status == UserStatus.Active);
        if (user is null)
        {
            return AuthenticateResult.Fail("API key owner is not active.");
        }

        matchedKey.LastUsedAt = DateTimeOffset.UtcNow;
        await unitOfWork.SaveChangesAsync(Context.RequestAborted);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.DisplayName),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim("auth_method", AppConstants.ApiKeyScheme)
        };
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        return AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name));
    }
}
