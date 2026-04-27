using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using UncannyPrompt.Domain;
using UncannyPrompt.Infrastructure;

namespace UncannyPrompt.WebApp.Security;

public sealed class TenantScopeProvider(
    UncannyPromptDbContext dbContext,
    IHttpContextAccessor httpContextAccessor) : ITenantScopeProvider
{
    private const string ScopeCookieName = "uncannyprompt_tenant_scope";
    private const string AllTenantsCookieValue = "all";

    public async Task<TenantScopeContext> GetScopeAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default)
    {
        var userId = ResolveUserId(principal);
        var isAdmin = userId is Guid id && await IsAdminAsync(id, cancellationToken);
        var accessibleTenantIds = await ResolveAccessibleTenantIdsAsync(userId, isAdmin, cancellationToken);

        if (accessibleTenantIds.Count == 0)
        {
            return new TenantScopeContext(Array.Empty<Guid>(), Array.Empty<Guid>(), null, null, null, false, isAdmin, null, false);
        }

        var requestedScope = httpContextAccessor.HttpContext?.Request.Cookies[ScopeCookieName];
        var scopePayload = ParseScopeCookie(requestedScope);
        var isAllTenants = isAdmin && string.Equals(requestedScope, AllTenantsCookieValue, StringComparison.OrdinalIgnoreCase);

        Guid? currentTenantId = null;
        Guid? currentWorkspaceId = null;
        Guid? currentProjectId = null;
        if (!isAllTenants)
        {
            if (scopePayload.TenantId is Guid requestedTenantId && accessibleTenantIds.Contains(requestedTenantId))
            {
                currentTenantId = requestedTenantId;
                currentWorkspaceId = scopePayload.WorkspaceId;
                currentProjectId = scopePayload.ProjectId;
            }
            else
            {
                currentTenantId = accessibleTenantIds[0];
            }
        }

        var effectiveTenantIds = isAllTenants
            ? accessibleTenantIds
            : currentTenantId is Guid tenantId
                ? new[] { tenantId }
                : Array.Empty<Guid>();

        TenantRole? currentTenantRole = null;
        var currentTenantCanGrant = false;
        if (!isAllTenants && currentTenantId is Guid selectedTenantId && userId is Guid actorUserId)
        {
            var membership = await dbContext.TenantMemberships
                .Where(x => x.UserId == actorUserId && x.TenantId == selectedTenantId && x.IsActive)
                .Select(x => new { x.Role, x.CanGrant })
                .FirstOrDefaultAsync(cancellationToken);
            if (membership is not null)
            {
                currentTenantRole = membership.Role;
                currentTenantCanGrant = membership.CanGrant;
            }
        }

        return new TenantScopeContext(
            accessibleTenantIds,
            effectiveTenantIds,
            currentTenantId,
            currentWorkspaceId,
            currentProjectId,
            isAllTenants,
            isAdmin,
            currentTenantRole,
            currentTenantCanGrant);
    }

    public async Task<bool> SetCurrentScopeAsync(ClaimsPrincipal principal, Guid tenantId, Guid? workspaceId, Guid? projectId, CancellationToken cancellationToken = default)
    {
        var userId = ResolveUserId(principal);
        var isAdmin = userId is Guid id && await IsAdminAsync(id, cancellationToken);
        var accessibleTenantIds = await ResolveAccessibleTenantIdsAsync(userId, isAdmin, cancellationToken);
        if (!accessibleTenantIds.Contains(tenantId))
        {
            return false;
        }

        var context = httpContextAccessor.HttpContext;
        if (context is null)
        {
            return false;
        }

        var payload = JsonSerializer.Serialize(new ScopeCookiePayload(tenantId, workspaceId, projectId));
        context.Response.Cookies.Append(ScopeCookieName, payload, BuildCookieOptions(context));
        return true;
    }

    public async Task<bool> SetCurrentTenantAsync(ClaimsPrincipal principal, Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await SetCurrentScopeAsync(principal, tenantId, null, null, cancellationToken);
    }

    public async Task<bool> SetAllTenantsAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default)
    {
        var userId = ResolveUserId(principal);
        var isAdmin = userId is Guid id && await IsAdminAsync(id, cancellationToken);
        if (!isAdmin)
        {
            return false;
        }

        var accessibleTenantIds = await ResolveAccessibleTenantIdsAsync(userId, isAdmin, cancellationToken);
        if (accessibleTenantIds.Count == 0)
        {
            return false;
        }

        var context = httpContextAccessor.HttpContext;
        if (context is null)
        {
            return false;
        }

        context.Response.Cookies.Append(ScopeCookieName, AllTenantsCookieValue, BuildCookieOptions(context));
        return true;
    }

    public void ClearScope()
    {
        var context = httpContextAccessor.HttpContext;
        if (context is null)
        {
            return;
        }

        context.Response.Cookies.Delete(ScopeCookieName, BuildCookieOptions(context));
    }

    private async Task<IReadOnlyList<Guid>> ResolveAccessibleTenantIdsAsync(Guid? userId, bool isAdmin, CancellationToken cancellationToken)
    {
        if (isAdmin)
        {
            return await dbContext.Tenants
                .Where(x => !x.IsDeleted)
                .OrderBy(x => x.Kind)
                .ThenBy(x => x.Name)
                .Select(x => x.Id)
                .ToListAsync(cancellationToken);
        }

        if (userId is not Guid id)
        {
            return Array.Empty<Guid>();
        }

        return await dbContext.TenantMemberships
            .Where(x => x.UserId == id && x.IsActive)
            .Join(dbContext.Tenants, m => m.TenantId, t => t.Id, (m, t) => new { m, t })
            .Where(x => !x.t.IsDeleted)
            .OrderBy(x => x.t.Kind)
            .ThenBy(x => x.t.Name)
            .Select(x => x.t.Id)
            .ToListAsync(cancellationToken);
    }

    private Task<bool> IsAdminAsync(Guid userId, CancellationToken cancellationToken) =>
        dbContext.Users.AnyAsync(x => x.Id == userId && x.Status == UserStatus.Active && x.Role == UserRole.Admin, cancellationToken);

    private static Guid? ResolveUserId(ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var id) ? id : null;
    }

    private static ScopeCookiePayload ParseScopeCookie(string? cookieValue)
    {
        if (string.IsNullOrWhiteSpace(cookieValue) || string.Equals(cookieValue, AllTenantsCookieValue, StringComparison.OrdinalIgnoreCase))
        {
            return ScopeCookiePayload.Empty;
        }

        if (Guid.TryParse(cookieValue, out var legacyTenantId))
        {
            return new ScopeCookiePayload(legacyTenantId, null, null);
        }

        try
        {
            return JsonSerializer.Deserialize<ScopeCookiePayload>(cookieValue) ?? ScopeCookiePayload.Empty;
        }
        catch (JsonException)
        {
            return ScopeCookiePayload.Empty;
        }
    }

    private static CookieOptions BuildCookieOptions(HttpContext context) => new()
    {
        HttpOnly = true,
        IsEssential = true,
        Path = "/",
        SameSite = SameSiteMode.Lax,
        Secure = context.Request.IsHttps,
        MaxAge = TimeSpan.FromDays(30)
    };

    private sealed record ScopeCookiePayload(Guid? TenantId, Guid? WorkspaceId, Guid? ProjectId)
    {
        public static ScopeCookiePayload Empty { get; } = new(null, null, null);
    }
}
