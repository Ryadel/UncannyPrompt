using UncannyPrompt.Application;
using UncannyPrompt.Domain;
using UncannyPrompt.UnitTests.TestSupport;

namespace UncannyPrompt.UnitTests.Application;

public sealed class PromptServiceTests
{
    [Fact]
    public async Task CreateAsync_CreatesPromptAndInitialImmutableVersion()
    {
        using var database = new TestDatabase();
        var (user, _, _, project) = await TestDataSeeder.SeedWorkspaceAsync(database.DbContext);
        var service = CreatePromptService(database);

        var prompt = await service.CreateAsync(user.Id, new PromptUpsertRequest(
            project.Id,
            null,
            "Summarize",
            "Short summary",
            "Summarize {{topic}}.",
            "Useful for briefs.",
            PromptStatus.Ready,
            ["summary", "template"],
            "initial",
            "Created prompt.",
            "v1 notes"));

        Assert.Equal(1, prompt.CurrentVersionNumber);
        Assert.Equal(2, prompt.Tags.Count);
        Assert.Single(database.DbContext.Prompts);
        Assert.Single(database.DbContext.PromptVersions);
        Assert.Contains(database.DbContext.AuditEvents, x => x.EventType == "prompt.create" && x.EntityId == prompt.Id);
    }

    [Fact]
    public async Task UpdateAsync_CreatesNewVersion_WhenContentChanges()
    {
        using var database = new TestDatabase();
        var (user, _, _, project) = await TestDataSeeder.SeedWorkspaceAsync(database.DbContext);
        var service = CreatePromptService(database);
        var prompt = await service.CreateAsync(user.Id, new PromptUpsertRequest(
            project.Id,
            null,
            "Draft",
            null,
            "Original content",
            null,
            PromptStatus.Draft));

        var updated = await service.UpdateAsync(user.Id, prompt.Id, new PromptUpsertRequest(
            project.Id,
            null,
            "Draft",
            null,
            "Updated content",
            null,
            PromptStatus.Ready,
            Changelog: "Improved wording."));

        Assert.Equal(2, updated.CurrentVersionNumber);
        Assert.Equal(2, database.DbContext.PromptVersions.Count(x => x.PromptId == prompt.Id));
        Assert.Contains(database.DbContext.PromptVersions, x => x.PromptId == prompt.Id && x.VersionNumber == 1 && x.Content == "Original content");
        Assert.Contains(database.DbContext.PromptVersions, x => x.PromptId == prompt.Id && x.VersionNumber == 2 && x.Content == "Updated content");
    }

    [Fact]
    public async Task LogCopyAsync_IncrementsCopyCountAndLastUsedAt()
    {
        using var database = new TestDatabase();
        var (user, _, _, project) = await TestDataSeeder.SeedWorkspaceAsync(database.DbContext);
        var service = CreatePromptService(database);
        var prompt = await service.CreateAsync(user.Id, new PromptUpsertRequest(
            project.Id,
            null,
            "Copy me",
            null,
            "Raw text",
            null,
            PromptStatus.Ready));

        await service.LogCopyAsync(user.Id, prompt.Id, resolved: true);
        var stored = database.DbContext.Prompts.Single(x => x.Id == prompt.Id);

        Assert.Equal(1, stored.CopyCount);
        Assert.NotNull(stored.LastUsedAt);
        Assert.Contains(database.DbContext.AuditEvents, x => x.EventType == "prompt.copy" && x.EntityId == prompt.Id);
    }

    private static PromptService CreatePromptService(TestDatabase database) =>
        new(database.UnitOfWork, CreatePermissionService(database), CreateAuditService(database));

    private static PermissionService CreatePermissionService(TestDatabase database) =>
        new(database.UnitOfWork, new TenantScopeService(database.UnitOfWork));

    private static AuditService CreateAuditService(TestDatabase database) =>
        new(database.UnitOfWork, CreatePermissionService(database));
}
