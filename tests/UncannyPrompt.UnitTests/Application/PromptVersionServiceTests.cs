using UncannyPrompt.Application;
using UncannyPrompt.Domain;
using UncannyPrompt.UnitTests.TestSupport;

namespace UncannyPrompt.UnitTests.Application;

public sealed class PromptVersionServiceTests
{
    [Fact]
    public async Task RestoreAsync_CreatesNewCurrentVersionWithoutMutatingHistory()
    {
        using var database = new TestDatabase();
        var (user, _, _, project) = await TestDataSeeder.SeedWorkspaceAsync(database.DbContext);
        var prompt = new Prompt
        {
            ProjectId = project.Id,
            AuthorUserId = user.Id,
            Title = "Versioned",
            Content = "Second content",
            CurrentVersionNumber = 2
        };
        var firstVersion = new PromptVersion
        {
            PromptId = prompt.Id,
            AuthorUserId = user.Id,
            VersionNumber = 1,
            Content = "First content"
        };
        var secondVersion = new PromptVersion
        {
            PromptId = prompt.Id,
            AuthorUserId = user.Id,
            VersionNumber = 2,
            Content = "Second content"
        };
        database.DbContext.Prompts.Add(prompt);
        database.DbContext.PromptVersions.AddRange(firstVersion, secondVersion);
        await database.SaveChangesAsync();
        var permissionService = new PermissionService(database.UnitOfWork, new TenantScopeService(database.UnitOfWork));
        var service = new PromptVersionService(database.UnitOfWork, permissionService, new AuditService(database.UnitOfWork, permissionService));

        var restored = await service.RestoreAsync(user.Id, prompt.Id, firstVersion.Id, "Restore first version.");

        Assert.Equal(3, restored.CurrentVersionNumber);
        Assert.Equal("First content", database.DbContext.Prompts.Single(x => x.Id == prompt.Id).Content);
        Assert.Equal(3, database.DbContext.PromptVersions.Count(x => x.PromptId == prompt.Id));
        Assert.Contains(database.DbContext.PromptVersions, x => x.PromptId == prompt.Id && x.VersionNumber == 1 && x.Content == "First content");
        Assert.Contains(database.DbContext.PromptVersions, x => x.PromptId == prompt.Id && x.VersionNumber == 2 && x.Content == "Second content");
        Assert.Contains(database.DbContext.PromptVersions, x => x.PromptId == prompt.Id && x.VersionNumber == 3 && x.Label == "restore-1");
    }
}
