using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using UncannyPrompt.Application;
using UncannyPrompt.Domain;
using UncannyPrompt.Infrastructure;
using UncannyPrompt.Shared;
using UncannyPrompt.UnitTests.TestSupport;
using UncannyPrompt.WebApp.Security;

namespace UncannyPrompt.UnitTests.Security;

public sealed class ApiKeyAuthenticationHandlerTests
{
    [Fact]
    public async Task ProtectedEndpoint_ReturnsUnauthorized_WhenApiKeyIsMissing()
    {
        using var host = await CreateHostAsync();
        using var client = host.GetTestClient();

        var response = await client.GetAsync("/secure");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_AuthenticatesUser_WhenApiKeyIsValid()
    {
        using var host = await CreateHostAsync();
        var apiKey = await SeedApiKeyAsync(host.Services);
        AssertSeededApiKeyCanBeVerified(host.Services, apiKey);
        using var client = host.GetTestClient();
        client.DefaultRequestHeaders.Add("X-API-Key", apiKey);

        var response = await client.GetAsync("/secure");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("api@example.test|ApiKey", body);
    }

    [Fact]
    public async Task ProtectedEndpoint_RejectsApiKey_WhenOwnerIsBlocked()
    {
        using var host = await CreateHostAsync();
        var apiKey = await SeedApiKeyAsync(host.Services, UserStatus.Blocked);
        using var client = host.GetTestClient();
        client.DefaultRequestHeaders.Add("X-API-Key", apiKey);

        var response = await client.GetAsync("/secure");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static Task<IHost> CreateHostAsync()
    {
        var databaseName = Guid.NewGuid().ToString();
        return new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder.UseTestServer();
                webBuilder.ConfigureServices(services =>
                {
                    services.AddRouting();
                    services.AddAuthorization();
                    services.AddDbContext<UncannyPromptDbContext>(options => options.UseInMemoryDatabase(databaseName));
                    services.AddScoped<IUnitOfWork, EfUnitOfWork>();
                    services.AddSingleton<IApiKeyHasher>(new ApiKeyHasher(TestConfiguration.Create()));
                    services.AddAuthentication(options =>
                    {
                        options.DefaultAuthenticateScheme = AppConstants.ApiKeyScheme;
                        options.DefaultChallengeScheme = AppConstants.ApiKeyScheme;
                    })
                    .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(AppConstants.ApiKeyScheme, _ => { });
                });
                webBuilder.Configure(app =>
                {
                    app.UseRouting();
                    app.UseAuthentication();
                    app.UseAuthorization();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapGet("/secure", async context =>
                        {
                            var email = context.User.FindFirstValue(ClaimTypes.Email);
                            var method = context.User.FindFirstValue("auth_method");
                            await context.Response.WriteAsync($"{email}|{method}");
                        }).RequireAuthorization(new AuthorizeAttribute { AuthenticationSchemes = AppConstants.ApiKeyScheme });
                    });
                });
            })
            .StartAsync();
    }

    private static async Task<string> SeedApiKeyAsync(IServiceProvider services, UserStatus status = UserStatus.Active)
    {
        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<UncannyPromptDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IApiKeyHasher>();
        var apiKey = "up_test_valid_key";
        var user = new User
        {
            DisplayName = "API User",
            Email = "api@example.test",
            Status = status
        };

        dbContext.Users.Add(user);
        dbContext.UserApiKeys.Add(new UserApiKey
        {
            UserId = user.Id,
            Name = "Tests",
            Prefix = "up_test",
            KeyHash = hasher.Hash(apiKey)
        });
        await dbContext.SaveChangesAsync();
        return apiKey;
    }

    private static void AssertSeededApiKeyCanBeVerified(IServiceProvider services, string apiKey)
    {
        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<UncannyPromptDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IApiKeyHasher>();
        var stored = dbContext.UserApiKeys.Single();

        Assert.True(hasher.Verify(apiKey, stored.KeyHash));
    }
}
