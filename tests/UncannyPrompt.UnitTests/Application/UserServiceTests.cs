using UncannyPrompt.Application;
using UncannyPrompt.Domain;
using UncannyPrompt.UnitTests.TestSupport;

namespace UncannyPrompt.UnitTests.Application;

public sealed class UserServiceTests
{
    [Fact]
    public async Task ProvisionExternalUserAsync_CreatesUserIdentityPersonalTenantAndStarterPrompt()
    {
        using var database = new TestDatabase();
        var auditService = CreateAuditService(database);
        var userService = new UserService(database.UnitOfWork, auditService);

        var user = await userService.ProvisionExternalUserAsync("github", "42", "Ada@Example.Test", "Ada", CancellationToken.None);

        Assert.Equal("ada@example.test", user.Email);
        Assert.Equal("Ada", user.DisplayName);
        Assert.Single(database.DbContext.AuthIdentities);
        var tenant = Assert.Single(database.DbContext.Tenants);
        Assert.Equal("Personal", tenant.Name);
        Assert.Single(database.DbContext.TenantMemberships);
        Assert.Single(database.DbContext.Workspaces);
        Assert.Single(database.DbContext.Projects);
        Assert.Single(database.DbContext.Prompts);
        Assert.Single(database.DbContext.PromptVersions);
        Assert.Contains(database.DbContext.AuditEvents, x => x.EventType == "login.success" && x.ActorUserId == user.Id);
    }

    [Fact]
    public async Task ProvisionExternalUserAsync_ReusesLinkedIdentity_WithoutDuplicatingPersonalTenant()
    {
        using var database = new TestDatabase();
        var auditService = CreateAuditService(database);
        var userService = new UserService(database.UnitOfWork, auditService);

        var firstLogin = await userService.ProvisionExternalUserAsync("google", "abc", "maker@example.test", "Maker", CancellationToken.None);
        var secondLogin = await userService.ProvisionExternalUserAsync("google", "abc", "maker@example.test", "Maker", CancellationToken.None);

        Assert.Equal(firstLogin.Id, secondLogin.Id);
        Assert.Single(database.DbContext.Users);
        Assert.Single(database.DbContext.AuthIdentities);
        Assert.Single(database.DbContext.Tenants);
        Assert.Single(database.DbContext.TenantMemberships);
    }

    [Fact]
    public async Task ProvisionExternalUserAsync_ThrowsAndAuditsDeniedLogin_WhenUserIsInactive()
    {
        using var database = new TestDatabase();
        var user = new User
        {
            DisplayName = "Blocked Maker",
            Email = "blocked@example.test",
            Status = UserStatus.Blocked
        };
        database.DbContext.Users.Add(user);
        database.DbContext.AuthIdentities.Add(new AuthIdentity
        {
            UserId = user.Id,
            Provider = "github",
            ProviderSubjectId = "blocked"
        });
        await database.SaveChangesAsync();
        var auditService = CreateAuditService(database);
        var userService = new UserService(database.UnitOfWork, auditService);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            userService.ProvisionExternalUserAsync("github", "blocked", user.Email, user.DisplayName, CancellationToken.None));

        Assert.Contains(database.DbContext.AuditEvents, x => x.EventType == "login.denied.inactive_user" && x.ActorUserId == user.Id);
    }

    private static AuditService CreateAuditService(TestDatabase database) =>
        new(database.UnitOfWork, new PermissionService(database.UnitOfWork, new TenantScopeService(database.UnitOfWork)));
}
