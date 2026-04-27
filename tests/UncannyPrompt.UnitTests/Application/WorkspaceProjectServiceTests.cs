using UncannyPrompt.Application;
using UncannyPrompt.Domain;
using UncannyPrompt.UnitTests.TestSupport;

namespace UncannyPrompt.UnitTests.Application;

public sealed class WorkspaceProjectServiceTests
{
    [Fact]
    public async Task WorkspaceCreateAsync_CreatesWorkspace_WhenTenantExists()
    {
        using var database = new TestDatabase();
        var (user, tenant, _, _) = await TestDataSeeder.SeedWorkspaceAsync(database.DbContext);
        var service = CreateWorkspaceService(database);

        var workspace = await service.CreateAsync(user.Id, tenant.Id, "Planning", "Editorial planning");

        Assert.Equal(tenant.Id, workspace.TenantId);
        Assert.Equal("Planning", workspace.Name);
        Assert.Contains(database.DbContext.Workspaces, x => x.Id == workspace.Id && x.TenantId == tenant.Id);
    }

    [Fact]
    public async Task WorkspaceCreateAsync_BlocksCreation_WhenTenantIsMissing()
    {
        using var database = new TestDatabase();
        var (user, _, _, _) = await TestDataSeeder.SeedWorkspaceAsync(database.DbContext);
        var service = CreateWorkspaceService(database);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.CreateAsync(user.Id, Guid.NewGuid(), "No tenant", null));

        Assert.DoesNotContain(database.DbContext.Workspaces, x => x.Name == "No tenant");
    }

    [Fact]
    public async Task ProjectCreateAsync_CreatesProject_WhenWorkspaceExists()
    {
        using var database = new TestDatabase();
        var (user, _, workspace, _) = await TestDataSeeder.SeedWorkspaceAsync(database.DbContext);
        var service = CreateProjectService(database);

        var project = await service.CreateAsync(user.Id, workspace.Id, "Launch", "Launch assets");

        Assert.Equal(workspace.Id, project.WorkspaceId);
        Assert.Equal("Launch", project.Name);
        Assert.Contains(database.DbContext.Projects, x => x.Id == project.Id && x.WorkspaceId == workspace.Id);
    }

    [Fact]
    public async Task ProjectCreateAsync_BlocksCreation_WhenWorkspaceIsMissing()
    {
        using var database = new TestDatabase();
        var (user, _, _, _) = await TestDataSeeder.SeedWorkspaceAsync(database.DbContext);
        var service = CreateProjectService(database);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateAsync(user.Id, Guid.NewGuid(), "No workspace", null));

        Assert.DoesNotContain(database.DbContext.Projects, x => x.Name == "No workspace");
    }

    private static WorkspaceService CreateWorkspaceService(TestDatabase database) =>
        new(database.UnitOfWork, CreatePermissionService(database));

    private static ProjectService CreateProjectService(TestDatabase database) =>
        new(database.UnitOfWork, CreatePermissionService(database));

    private static PermissionService CreatePermissionService(TestDatabase database) =>
        new(database.UnitOfWork, new TenantScopeService(database.UnitOfWork));
}
