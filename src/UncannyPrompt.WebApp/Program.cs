using System.Security.Claims;
using System.Net;
using AspNet.Security.OAuth.GitHub;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Serilog;
using UncannyPrompt.Application;
using UncannyPrompt.Infrastructure;
using UncannyPrompt.Shared;
using UncannyPrompt.WebApp;
using UncannyPrompt.WebApp.Controllers;
using UncannyPrompt.WebApp.Extensions;
using UncannyPrompt.WebApp.Security;

var builder = WebApplication.CreateBuilder(args);
builder.ConfigureUncannyPromptHost();

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.ForwardLimit = Math.Max(1, builder.Configuration.GetValue<int?>("ReverseProxy:ForwardLimit") ?? 1);

    var knownProxies = builder.Configuration.GetSection("ReverseProxy:KnownProxies").Get<string[]>() ?? Array.Empty<string>();
    foreach (var proxy in knownProxies)
    {
        if (IPAddress.TryParse(proxy, out var ipAddress))
        {
            options.KnownProxies.Add(ipAddress);
        }
    }

    var knownNetworks = builder.Configuration.GetSection("ReverseProxy:KnownNetworks").Get<string[]>() ?? Array.Empty<string>();
    foreach (var network in knownNetworks)
    {
        var parts = network.Split('/', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
        {
            continue;
        }

        if (!IPAddress.TryParse(parts[0], out var address) || !int.TryParse(parts[1], out var prefixLength))
        {
            continue;
        }

        var maxPrefix = address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork ? 32 : 128;
        if (prefixLength < 0 || prefixLength > maxPrefix)
        {
            continue;
        }

        options.KnownIPNetworks.Add(new System.Net.IPNetwork(address, prefixLength));
    }
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserContext, CurrentUserContext>();
builder.Services.AddScoped<ITenantScopeProvider, TenantScopeProvider>();
builder.Services.AddScoped<IAppScopeService, AppScopeService>();
builder.Services.AddScoped<IAuthorizationHandler, AdminRequirementHandler>();
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();
builder.Services.AddLocalization(options =>
{
    options.ResourcesPath = "Resources";
});
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
});
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AllowAnonymousToPage("/Index");
    options.Conventions.AllowAnonymousToFolder("/Help");
    options.Conventions.AuthorizePage("/Prompts");
    options.Conventions.AuthorizeAreaFolder("Admin", "/", AppConstants.AdminOnlyPolicy);
    options.Conventions.AllowAnonymousToPage("/Account/Login");
    options.Conventions.AllowAnonymousToPage("/PublicPrompt");
})
.AddViewLocalization()
.AddDataAnnotationsLocalization(options =>
{
    options.DataAnnotationLocalizerProvider = (_, factory) => factory.Create(typeof(SharedResource));
});
builder.Services.AddControllers(options =>
{
    options.Filters.Add(new ProducesAttribute("application/json"));
    options.Filters.Add<CookieAntiforgeryFilter>();
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("api", limiter =>
    {
        limiter.PermitLimit = 120;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueLimit = 0;
    });
    options.AddFixedWindowLimiter("login", limiter =>
    {
        limiter.PermitLimit = 12;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueLimit = 0;
    });
});

var consoleTelemetryEnabled = builder.Configuration.GetValue<bool>("OpenTelemetry:ConsoleExporter:Enabled");

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation();

        if (consoleTelemetryEnabled)
        {
            tracing.AddConsoleExporter();
        }
    })
    .WithMetrics(metrics =>
    {
        metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation();

        if (consoleTelemetryEnabled)
        {
            metrics.AddConsoleExporter();
        }
    });

var authentication = builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = AppConstants.CookieScheme;
    options.DefaultChallengeScheme = AppConstants.CookieScheme;
});

authentication.AddCookie(AppConstants.CookieScheme, options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/Login";
    options.Cookie.Name = "__Host-UncannyPrompt";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
});
authentication.AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(AppConstants.ApiKeyScheme, _ => { });

AddExternalProviderIfConfigured("Authentication:Google", () =>
    authentication.AddGoogle(options =>
    {
        builder.Configuration.GetSection("Authentication:Google").Bind(options);
        options.Events.OnCreatingTicket = context => ProvisionExternalUserAsync(context, "google");
    }));

AddExternalProviderIfConfigured("Authentication:EntraId", () =>
{
    authentication.AddMicrosoftIdentityWebApp(
        builder.Configuration.GetSection("Authentication:EntraId"),
        openIdConnectScheme: OpenIdConnectDefaults.AuthenticationScheme,
        cookieScheme: null);
    builder.Services.Configure<OpenIdConnectOptions>(OpenIdConnectDefaults.AuthenticationScheme, options =>
    {
        options.SignInScheme = AppConstants.CookieScheme;
        options.ResponseType = OpenIdConnectResponseType.Code;
        options.Events ??= new OpenIdConnectEvents();
        options.Events.OnRedirectToIdentityProvider = context =>
        {
            GetEntraLogger(context.HttpContext).LogInformation(
                "Redirecting to Microsoft Entra ID. RedirectUri: {RedirectUri}; ResponseType: {ResponseType}; Scope: {Scope}; Request: {Scheme}://{Host}{PathBase}{Path}",
                context.ProtocolMessage.RedirectUri,
                context.ProtocolMessage.ResponseType,
                context.ProtocolMessage.Scope,
                context.Request.Scheme,
                context.Request.Host.Value,
                context.Request.PathBase.Value,
                context.Request.Path.Value);
            return Task.CompletedTask;
        };
        options.Events.OnMessageReceived = context =>
        {
            var hasError = !string.IsNullOrWhiteSpace(context.ProtocolMessage.Error);
            var logger = GetEntraLogger(context.HttpContext);

            if (hasError)
            {
                logger.LogWarning(
                    "Microsoft Entra ID callback returned an error. Error: {Error}; Description: {ErrorDescription}; ErrorUri: {ErrorUri}; HasCode: {HasCode}; HasState: {HasState}; Method: {Method}; Path: {Path}",
                    context.ProtocolMessage.Error,
                    Truncate(context.ProtocolMessage.ErrorDescription),
                    context.ProtocolMessage.ErrorUri,
                    !string.IsNullOrWhiteSpace(context.ProtocolMessage.Code),
                    !string.IsNullOrWhiteSpace(context.ProtocolMessage.State),
                    context.Request.Method,
                    context.Request.Path.Value);
            }
            else
            {
                logger.LogInformation(
                    "Microsoft Entra ID callback received. HasCode: {HasCode}; HasState: {HasState}; Method: {Method}; Path: {Path}",
                    !string.IsNullOrWhiteSpace(context.ProtocolMessage.Code),
                    !string.IsNullOrWhiteSpace(context.ProtocolMessage.State),
                    context.Request.Method,
                    context.Request.Path.Value);
            }

            return Task.CompletedTask;
        };
        options.Events.OnTokenValidated = async context =>
        {
            GetEntraLogger(context.HttpContext).LogInformation(
                "Microsoft Entra ID token validated. HasOid: {HasOid}; HasSub: {HasSub}; HasEmail: {HasEmail}; HasName: {HasName}",
                context.Principal?.FindFirst("oid") is not null,
                context.Principal?.FindFirst("sub") is not null,
                context.Principal?.FindFirst("preferred_username") is not null ||
                context.Principal?.FindFirst("email") is not null ||
                context.Principal?.FindFirst(ClaimTypes.Email) is not null,
                context.Principal?.FindFirst("name") is not null ||
                context.Principal?.FindFirst(ClaimTypes.Name) is not null);

            await ProvisionEntraUserAsync(context);
        };
        options.Events.OnAuthenticationFailed = context =>
        {
            GetEntraLogger(context.HttpContext).LogError(
                context.Exception,
                "Microsoft Entra ID callback authentication failed. Method: {Method}; Path: {Path}; Message: {Message}",
                context.Request.Method,
                context.Request.Path.Value,
                Truncate(context.Exception.Message));
            context.Response.Redirect("/Account/Login?error=entra");
            context.HandleResponse();
            return Task.CompletedTask;
        };
        options.Events.OnRemoteFailure = context =>
        {
            GetEntraLogger(context.HttpContext).LogError(
                context.Failure,
                "Microsoft Entra ID remote authentication failed. Method: {Method}; Path: {Path}; Failure: {Failure}",
                context.Request.Method,
                context.Request.Path.Value,
                Truncate(context.Failure?.Message));
            context.Response.Redirect("/Account/Login?error=entra");
            context.HandleResponse();
            return Task.CompletedTask;
        };
    });
}, "TenantId");

AddExternalProviderIfConfigured("Authentication:GitHub", () =>
    authentication.AddGitHub(options =>
    {
        builder.Configuration.GetSection("Authentication:GitHub").Bind(options);
        options.Scope.Add("user:email");
        options.Events.OnCreatingTicket = context => ProvisionExternalUserAsync(context, "github");
    }));

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AppConstants.AdminOnlyPolicy, policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.Requirements.Add(new AdminRequirement());
    });
});

var app = builder.Build();
var supportedCultures = new[] { "en", "it" };

await app.InitializeUncannyPromptDatabaseAndSeedAsync(builder.Configuration);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseForwardedHeaders();
app.UseHttpsRedirection();

app.UseRequestLocalization(new RequestLocalizationOptions()
    .SetDefaultCulture("en")
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures));
app.UseSerilogRequestLogging();
app.UseStatusCodePages();
var isDevelopmentEnvironment = app.Environment.IsDevelopment();
var cspConnectSrc = isDevelopmentEnvironment
    ? "'self' ws: wss: http://localhost:* https://localhost:*"
    : "'self'";
var contentSecurityPolicy =
    "default-src 'self'; " +
    "script-src 'self' 'unsafe-inline' 'unsafe-eval' https://unpkg.com; " +
    "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; " +
    "font-src 'self' https://fonts.gstatic.com; " +
    "img-src 'self' data:; " +
    $"connect-src {cspConnectSrc};";

app.Use(async (context, next) =>
{
    context.Response.Headers.TryAdd("X-Content-Type-Options", "nosniff");
    context.Response.Headers.TryAdd("X-Frame-Options", "DENY");
    context.Response.Headers.TryAdd("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.TryAdd("Content-Security-Policy", contentSecurityPolicy);
    await next();
});
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapControllers().RequireRateLimiting("api");
app.MapSwagger().RequireAuthorization();
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/swagger") &&
        context.User.Identity?.IsAuthenticated != true)
    {
        await context.ChallengeAsync();
        return;
    }

    await next();
});
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "UncannyPrompt API v1");
});
app.MapStaticAssets();
app.MapRazorPages().WithStaticAssets();

app.Run();

void AddExternalProviderIfConfigured(string sectionName, Action register, params string[] additionalRequiredKeys)
{
    var section = builder.Configuration.GetSection(sectionName);
    if (!string.IsNullOrWhiteSpace(section["ClientId"]) &&
        !string.IsNullOrWhiteSpace(section["ClientSecret"]) &&
        additionalRequiredKeys.All(key => !string.IsNullOrWhiteSpace(section[key])))
    {
        register();
    }
}

static async Task ProvisionExternalUserAsync(OAuthCreatingTicketContext context, string provider)
{
    var externalId = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? context.Principal?.FindFirstValue("urn:github:id")
        ?? throw new InvalidOperationException("External provider did not return a subject id.");
    var email = context.Principal?.FindFirstValue(ClaimTypes.Email)
        ?? context.Principal?.FindFirstValue("urn:github:email");
    var displayName = context.Principal?.FindFirstValue(ClaimTypes.Name)
        ?? context.Principal?.FindFirstValue("urn:github:name")
        ?? email;

    var userService = context.HttpContext.RequestServices.GetRequiredService<IUserService>();
    var user = await userService.ProvisionExternalUserAsync(provider, externalId, email, displayName, context.HttpContext.RequestAborted);

    if (context.Principal?.Identity is ClaimsIdentity identity)
    {
        foreach (var claim in identity.FindAll(ClaimTypes.NameIdentifier).ToList())
        {
            identity.RemoveClaim(claim);
        }

        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()));
        identity.AddClaim(new Claim(ClaimTypes.Email, user.Email));
        identity.AddClaim(new Claim(ClaimTypes.Name, user.DisplayName));
        identity.AddClaim(new Claim(ClaimTypes.Role, user.Role.ToString()));
    }
}

static async Task ProvisionEntraUserAsync(TokenValidatedContext context)
{
    var externalId = context.Principal?.FindFirstValue("oid")
        ?? context.Principal?.FindFirstValue("sub")
        ?? context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException("Entra ID did not return a subject id.");
    var email = context.Principal?.FindFirstValue("preferred_username")
        ?? context.Principal?.FindFirstValue("email")
        ?? context.Principal?.FindFirstValue(ClaimTypes.Email);
    var displayName = context.Principal?.FindFirstValue("name")
        ?? context.Principal?.FindFirstValue(ClaimTypes.Name)
        ?? email;

    var userService = context.HttpContext.RequestServices.GetRequiredService<IUserService>();
    var user = await userService.ProvisionExternalUserAsync("entra", externalId, email, displayName, context.HttpContext.RequestAborted);

    if (context.Principal?.Identity is ClaimsIdentity identity)
    {
        foreach (var claim in identity.FindAll(ClaimTypes.NameIdentifier).ToList())
        {
            identity.RemoveClaim(claim);
        }

        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()));
        identity.AddClaim(new Claim(ClaimTypes.Email, user.Email));
        identity.AddClaim(new Claim(ClaimTypes.Name, user.DisplayName));
        identity.AddClaim(new Claim(ClaimTypes.Role, user.Role.ToString()));
    }
}

static Microsoft.Extensions.Logging.ILogger GetEntraLogger(HttpContext httpContext) =>
    httpContext.RequestServices
        .GetRequiredService<ILoggerFactory>()
        .CreateLogger("UncannyPrompt.EntraId");

static string? Truncate(string? value, int maxLength = 700)
{
    if (string.IsNullOrWhiteSpace(value) || value.Length <= maxLength)
    {
        return value;
    }

    return value[..maxLength] + "...";
}
