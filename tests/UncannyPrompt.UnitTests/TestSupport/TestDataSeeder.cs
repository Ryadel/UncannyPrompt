using UncannyPrompt.Domain;
using UncannyPrompt.Infrastructure;

namespace UncannyPrompt.UnitTests.TestSupport;

internal static class TestDataSeeder
{
    public static async Task<(User User, Tenant Tenant, Workspace Workspace, Project Project)> SeedWorkspaceAsync(UncannyPromptDbContext dbContext)
    {
        var user = new User
        {
            DisplayName = "Ada Lovelace",
            Email = "ada@example.test",
            Status = UserStatus.Active
        };
        var tenant = new Tenant
        {
            Name = "Ada prompts",
            Kind = TenantKind.Personal,
            OwnerUserId = user.Id
        };
        var workspace = new Workspace
        {
            TenantId = tenant.Id,
            Name = "Research"
        };
        var project = new Project
        {
            WorkspaceId = workspace.Id,
            Name = "Experiments"
        };

        dbContext.Users.Add(user);
        dbContext.Tenants.Add(tenant);
        dbContext.TenantMemberships.Add(new TenantMembership
        {
            TenantId = tenant.Id,
            UserId = user.Id,
            Role = TenantRole.TenantOwner,
            CanGrant = true
        });
        dbContext.Workspaces.Add(workspace);
        dbContext.Projects.Add(project);
        await dbContext.SaveChangesAsync();
        return (user, tenant, workspace, project);
    }
}
