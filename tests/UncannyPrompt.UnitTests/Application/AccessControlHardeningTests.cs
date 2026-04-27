using UncannyPrompt.Application;
using UncannyPrompt.Domain;
using UncannyPrompt.Infrastructure;
using UncannyPrompt.UnitTests.TestSupport;

namespace UncannyPrompt.UnitTests.Application;

public sealed class AccessControlHardeningTests
{
    [Fact]
    public async Task CanAccessProjectAsync_DoesNotTreatViewerMembershipAsEditPermission()
    {
        using var database = new TestDatabase();
        var (_, tenant, _, project) = await TestDataSeeder.SeedWorkspaceAsync(database.DbContext);
        var viewer = new User { DisplayName = "Viewer", Email = "viewer@example.test" };
        database.DbContext.Users.Add(viewer);
        database.DbContext.TenantMemberships.Add(new TenantMembership
        {
            TenantId = tenant.Id,
            UserId = viewer.Id,
            Role = TenantRole.Viewer
        });
        await database.SaveChangesAsync();
        var scopeService = new TenantScopeService(database.UnitOfWork);

        Assert.True(await scopeService.CanAccessProjectAsync(viewer.Id, project.Id));
        Assert.False(await scopeService.CanAccessProjectAsync(viewer.Id, project.Id, SharePermission.Edit));
    }

    [Fact]
    public async Task CanAccessPromptAsync_HonorsDirectPromptGrantWithoutProjectMembership()
    {
        using var database = new TestDatabase();
        var (owner, _, _, project) = await TestDataSeeder.SeedWorkspaceAsync(database.DbContext);
        var guest = new User { DisplayName = "Guest", Email = "guest@example.test" };
        var prompt = new Prompt
        {
            ProjectId = project.Id,
            AuthorUserId = owner.Id,
            Title = "Shared prompt",
            Content = "Prompt content"
        };
        database.DbContext.Users.Add(guest);
        database.DbContext.Prompts.Add(prompt);
        database.DbContext.ShareGrants.Add(new ShareGrant
        {
            TargetType = ShareTargetType.Prompt,
            TargetId = prompt.Id,
            UserId = guest.Id,
            Permission = SharePermission.Edit
        });
        await database.SaveChangesAsync();
        var scopeService = new TenantScopeService(database.UnitOfWork);

        Assert.False(await scopeService.CanAccessProjectAsync(guest.Id, project.Id));
        Assert.True(await scopeService.CanAccessPromptAsync(guest.Id, prompt.Id, SharePermission.Edit));
    }

    [Fact]
    public async Task PermissionService_DoesNotTreatEditGrantAsPublishShareOrDelete()
    {
        using var database = new TestDatabase();
        var (owner, _, _, project) = await TestDataSeeder.SeedWorkspaceAsync(database.DbContext);
        var guest = new User { DisplayName = "Editor Guest", Email = "editor.guest@example.test" };
        var prompt = new Prompt
        {
            ProjectId = project.Id,
            AuthorUserId = owner.Id,
            Title = "Shared editor prompt",
            Content = "Prompt content"
        };
        database.DbContext.Users.Add(guest);
        database.DbContext.Prompts.Add(prompt);
        database.DbContext.ShareGrants.Add(new ShareGrant
        {
            TargetType = ShareTargetType.Prompt,
            TargetId = prompt.Id,
            UserId = guest.Id,
            Permission = SharePermission.Edit
        });
        await database.SaveChangesAsync();
        var permissionService = CreatePermissionService(database);

        Assert.True(await permissionService.CanAccessPromptAsync(guest.Id, prompt.Id, ApplicationPermission.PromptEdit));
        Assert.False(await permissionService.CanAccessPromptAsync(guest.Id, prompt.Id, ApplicationPermission.PromptPublish));
        Assert.False(await permissionService.CanAccessPromptAsync(guest.Id, prompt.Id, ApplicationPermission.PromptShare));
        Assert.False(await permissionService.CanAccessPromptAsync(guest.Id, prompt.Id, ApplicationPermission.PromptDelete));
    }

    [Fact]
    public async Task PermissionService_ProjectContributorCanEditButCannotPublish()
    {
        using var database = new TestDatabase();
        var (_, tenant, _, project) = await TestDataSeeder.SeedWorkspaceAsync(database.DbContext);
        var contributor = new User { DisplayName = "Contributor", Email = "contributor@example.test" };
        database.DbContext.Users.Add(contributor);
        database.DbContext.TenantMemberships.Add(new TenantMembership
        {
            TenantId = tenant.Id,
            UserId = contributor.Id,
            Role = TenantRole.ProjectContributor
        });
        await database.SaveChangesAsync();
        var permissionService = CreatePermissionService(database);

        Assert.True(await permissionService.CanAccessProjectAsync(contributor.Id, project.Id, ApplicationPermission.PromptEdit));
        Assert.False(await permissionService.CanAccessProjectAsync(contributor.Id, project.Id, ApplicationPermission.PromptPublish));
        Assert.False(await permissionService.CanAccessProjectAsync(contributor.Id, project.Id, ApplicationPermission.PromptShare));
    }

    [Fact]
    public async Task PermissionService_PlatformAdminHasGlobalResourcePermissions()
    {
        using var database = new TestDatabase();
        var (_, tenant, _, project) = await TestDataSeeder.SeedWorkspaceAsync(database.DbContext);
        var platformOwner = new User
        {
            DisplayName = "Platform Owner",
            Email = "platform.owner@example.test",
            Role = UserRole.Admin
        };
        database.DbContext.Users.Add(platformOwner);
        await database.SaveChangesAsync();
        var permissionService = CreatePermissionService(database);
        var projectService = new ProjectService(database.UnitOfWork, permissionService);

        Assert.True(await permissionService.CanAccessTenantAsync(platformOwner.Id, tenant.Id, ApplicationPermission.AuditView));
        Assert.True(await permissionService.CanAccessProjectAsync(platformOwner.Id, project.Id, ApplicationPermission.ProjectManage));
        Assert.Contains(await projectService.ListAsync(platformOwner.Id), x => x.Id == project.Id);
    }

    [Fact]
    public async Task PromptListAsync_HonorsInheritedFolderGrantWithoutScanningUnauthorizedPromptsIntoResult()
    {
        using var database = new TestDatabase();
        var (owner, _, _, project) = await TestDataSeeder.SeedWorkspaceAsync(database.DbContext);
        var guest = new User { DisplayName = "Folder Guest", Email = "folder.guest@example.test" };
        var parent = new Folder { ProjectId = project.Id, Name = "Parent" };
        var child = new Folder { ProjectId = project.Id, ParentFolderId = parent.Id, Name = "Child" };
        var sharedPrompt = new Prompt
        {
            ProjectId = project.Id,
            FolderId = child.Id,
            AuthorUserId = owner.Id,
            Title = "Visible",
            Content = "Visible content"
        };
        var hiddenPrompt = new Prompt
        {
            ProjectId = project.Id,
            AuthorUserId = owner.Id,
            Title = "Hidden",
            Content = "Hidden content"
        };
        database.DbContext.Users.Add(guest);
        database.DbContext.Folders.AddRange(parent, child);
        database.DbContext.Prompts.AddRange(sharedPrompt, hiddenPrompt);
        database.DbContext.ShareGrants.Add(new ShareGrant
        {
            TargetType = ShareTargetType.Folder,
            TargetId = parent.Id,
            UserId = guest.Id,
            Permission = SharePermission.View,
            InheritsPermissions = true
        });
        await database.SaveChangesAsync();
        var service = CreatePromptService(database);

        var prompts = await service.ListAsync(guest.Id, new PromptSearchRequest());

        Assert.Single(prompts);
        Assert.Equal(sharedPrompt.Id, prompts[0].Id);
    }

    [Fact]
    public async Task SharingListAsync_ReturnsOnlyGrantsOnShareableResources()
    {
        using var database = new TestDatabase();
        var (owner, _, _, ownerProject) = await TestDataSeeder.SeedWorkspaceAsync(database.DbContext);
        var otherOwner = new User { DisplayName = "Other Owner", Email = "other.owner@example.test" };
        var otherTenant = new Tenant { Name = "Other tenant", Kind = TenantKind.Personal, OwnerUserId = otherOwner.Id };
        var otherWorkspace = new Workspace { TenantId = otherTenant.Id, Name = "Other workspace" };
        var otherProject = new Project { WorkspaceId = otherWorkspace.Id, Name = "Other project" };
        var recipient = new User { DisplayName = "Recipient", Email = "recipient@example.test" };
        database.DbContext.Users.AddRange(otherOwner, recipient);
        database.DbContext.Tenants.Add(otherTenant);
        database.DbContext.TenantMemberships.Add(new TenantMembership
        {
            TenantId = otherTenant.Id,
            UserId = otherOwner.Id,
            Role = TenantRole.TenantOwner,
            CanGrant = true
        });
        database.DbContext.Workspaces.Add(otherWorkspace);
        database.DbContext.Projects.Add(otherProject);
        var visibleGrant = new ShareGrant
        {
            TargetType = ShareTargetType.Project,
            TargetId = ownerProject.Id,
            UserId = recipient.Id,
            Permission = SharePermission.View
        };
        var hiddenGrant = new ShareGrant
        {
            TargetType = ShareTargetType.Project,
            TargetId = otherProject.Id,
            UserId = recipient.Id,
            Permission = SharePermission.View
        };
        database.DbContext.ShareGrants.AddRange(visibleGrant, hiddenGrant);
        await database.SaveChangesAsync();
        var sharingService = new SharingService(
            database.UnitOfWork,
            CreatePermissionService(database),
            new PublicTokenHasher(TestConfiguration.Create()),
            CreateAuditService(database));

        var grants = await sharingService.ListAsync(owner.Id);

        Assert.Contains(grants, x => x.Id == visibleGrant.Id);
        Assert.DoesNotContain(grants, x => x.Id == hiddenGrant.Id);
        Assert.False(await new TenantScopeService(database.UnitOfWork).CanAccessProjectAsync(otherOwner.Id, ownerProject.Id));
    }

    [Fact]
    public async Task GetPublicPromptAsync_UsesLookupHashAndAuditsAccess()
    {
        using var database = new TestDatabase();
        var (owner, _, _, project) = await TestDataSeeder.SeedWorkspaceAsync(database.DbContext);
        var prompt = new Prompt
        {
            ProjectId = project.Id,
            AuthorUserId = owner.Id,
            Title = "Public",
            Content = "Public content"
        };
        database.DbContext.Prompts.Add(prompt);
        await database.SaveChangesAsync();
        var sharingService = new SharingService(
            database.UnitOfWork,
            CreatePermissionService(database),
            new PublicTokenHasher(TestConfiguration.Create()),
            CreateAuditService(database));

        var link = await sharingService.CreatePublicLinkAsync(owner.Id, prompt.Id, expiresAt: null);
        var storedLink = database.DbContext.PublicShareLinks.Single(x => x.Id == link.Id);
        var publicPrompt = await sharingService.GetPublicPromptAsync(link.Token);

        Assert.NotNull(publicPrompt);
        Assert.Equal(prompt.Id, publicPrompt.Id);
        Assert.False(string.IsNullOrWhiteSpace(storedLink.TokenLookupHash));
        Assert.NotEqual(storedLink.TokenLookupHash, storedLink.TokenHash);
        Assert.Contains(database.DbContext.AuditEvents, x => x.EventType == "public_link.access" && x.EntityId == link.Id);
    }

    [Fact]
    public async Task GetPublicPromptAsync_ReturnsNullWithoutAudit_WhenTokenDoesNotMatch()
    {
        using var database = new TestDatabase();
        var sharingService = CreateSharingService(database);

        var publicPrompt = await sharingService.GetPublicPromptAsync("not-a-real-token");

        Assert.Null(publicPrompt);
        Assert.DoesNotContain(database.DbContext.AuditEvents, x => x.EventType == "public_link.access");
    }

    [Theory]
    [InlineData("revoked")]
    [InlineData("expired")]
    [InlineData("deleted")]
    public async Task GetPublicPromptAsync_ReturnsNullWithoutAudit_WhenLinkIsInactive(string inactiveState)
    {
        using var database = new TestDatabase();
        var (owner, _, _, project) = await TestDataSeeder.SeedWorkspaceAsync(database.DbContext);
        var prompt = new Prompt
        {
            ProjectId = project.Id,
            AuthorUserId = owner.Id,
            Title = "Public",
            Content = "Public content"
        };
        database.DbContext.Prompts.Add(prompt);
        await database.SaveChangesAsync();
        var sharingService = CreateSharingService(database);
        var link = await sharingService.CreatePublicLinkAsync(owner.Id, prompt.Id, expiresAt: null);
        var storedLink = database.DbContext.PublicShareLinks.Single(x => x.Id == link.Id);
        switch (inactiveState)
        {
            case "revoked":
                storedLink.RevokedAt = DateTimeOffset.UtcNow;
                break;
            case "expired":
                storedLink.ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1);
                break;
            case "deleted":
                storedLink.IsDeleted = true;
                storedLink.DeletedAt = DateTimeOffset.UtcNow;
                break;
        }

        await database.SaveChangesAsync();
        var auditCountBefore = database.DbContext.AuditEvents.Count(x => x.EventType == "public_link.access");

        var publicPrompt = await sharingService.GetPublicPromptAsync(link.Token);

        Assert.Null(publicPrompt);
        Assert.Equal(auditCountBefore, database.DbContext.AuditEvents.Count(x => x.EventType == "public_link.access"));
    }

    private static PromptService CreatePromptService(TestDatabase database) =>
        new(database.UnitOfWork, CreatePermissionService(database), CreateAuditService(database));

    private static SharingService CreateSharingService(TestDatabase database) =>
        new(
            database.UnitOfWork,
            CreatePermissionService(database),
            new PublicTokenHasher(TestConfiguration.Create()),
            CreateAuditService(database));

    private static PermissionService CreatePermissionService(TestDatabase database) =>
        new(database.UnitOfWork, new TenantScopeService(database.UnitOfWork));

    private static AuditService CreateAuditService(TestDatabase database) =>
        new(database.UnitOfWork, CreatePermissionService(database));
}
