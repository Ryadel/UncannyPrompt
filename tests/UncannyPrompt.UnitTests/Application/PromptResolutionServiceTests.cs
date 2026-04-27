using UncannyPrompt.Application;
using UncannyPrompt.Domain;
using UncannyPrompt.Infrastructure;
using UncannyPrompt.UnitTests.TestSupport;

namespace UncannyPrompt.UnitTests.Application;

public sealed class PromptResolutionServiceTests
{
    [Fact]
    public async Task ResolveAsync_UsesUserProvidedValuesBeforeStoredDefaults()
    {
        using var database = new TestDatabase();
        var (user, _, _, project) = await TestDataSeeder.SeedWorkspaceAsync(database.DbContext);
        var prompt = new Prompt
        {
            ProjectId = project.Id,
            AuthorUserId = user.Id,
            Title = "Brief",
            Content = "Write for {{company_name}} about {{product_name}}."
        };
        var company = new VariableDefinition
        {
            UserId = user.Id,
            Scope = VariableScope.User,
            Name = "company_name",
            DefaultValue = "StoredCo",
            IsRequired = true
        };
        var product = new VariableDefinition
        {
            ProjectId = project.Id,
            Scope = VariableScope.Project,
            Name = "product_name",
            DefaultValue = "StoredProduct",
            IsRequired = true
        };
        database.DbContext.Prompts.Add(prompt);
        database.DbContext.VariableDefinitions.AddRange(company, product);
        await database.SaveChangesAsync();
        var service = CreateService(database);

        var result = await service.ResolveAsync(user.Id, prompt.Id, new Dictionary<string, string?>
        {
            ["company_name"] = "DirectCo"
        });

        Assert.Equal("Write for DirectCo about StoredProduct.", result.ResolvedContent);
        Assert.Empty(result.MissingVariables);
    }

    [Fact]
    public async Task ResolveAsync_ReportsMissingPlaceholders()
    {
        using var database = new TestDatabase();
        var (user, _, _, project) = await TestDataSeeder.SeedWorkspaceAsync(database.DbContext);
        var prompt = new Prompt
        {
            ProjectId = project.Id,
            AuthorUserId = user.Id,
            Title = "Brief",
            Content = "Write for {{company_name}} and {{missing_value}}."
        };
        database.DbContext.Prompts.Add(prompt);
        database.DbContext.VariableDefinitions.Add(new VariableDefinition
        {
            UserId = user.Id,
            Scope = VariableScope.User,
            Name = "company_name",
            DefaultValue = "StoredCo"
        });
        await database.SaveChangesAsync();
        var service = CreateService(database);

        var result = await service.ResolveAsync(user.Id, prompt.Id, new Dictionary<string, string?>());

        Assert.Equal("Write for StoredCo and {{missing_value}}.", result.ResolvedContent);
        Assert.Equal(["missing_value"], result.MissingVariables);
    }

    [Fact]
    public async Task ResolveAsync_UnprotectsSecretStoredValues()
    {
        using var database = new TestDatabase();
        var protector = new AesSecretProtector(TestConfiguration.Create());
        var (user, _, _, project) = await TestDataSeeder.SeedWorkspaceAsync(database.DbContext);
        var prompt = new Prompt
        {
            ProjectId = project.Id,
            AuthorUserId = user.Id,
            Title = "Secret",
            Content = "Use {{api_token}}."
        };
        var definition = new VariableDefinition
        {
            UserId = user.Id,
            Scope = VariableScope.User,
            Name = "api_token",
            IsSecret = true
        };
        database.DbContext.Prompts.Add(prompt);
        database.DbContext.VariableDefinitions.Add(definition);
        database.DbContext.VariableValues.Add(new VariableValue
        {
            VariableDefinitionId = definition.Id,
            UserId = user.Id,
            Value = protector.Protect("secret-token"),
            IsEncrypted = true
        });
        await database.SaveChangesAsync();
        var service = CreateService(database, protector);

        var result = await service.ResolveAsync(user.Id, prompt.Id, new Dictionary<string, string?>());

        Assert.Equal("Use secret-token.", result.ResolvedContent);
    }

    [Fact]
    public async Task ResolveAsync_PlatformAdminDoesNotReadAnotherUsersEncryptedUserVariable()
    {
        using var database = new TestDatabase();
        var protector = new AesSecretProtector(TestConfiguration.Create());
        var (owner, _, _, project) = await TestDataSeeder.SeedWorkspaceAsync(database.DbContext);
        var platformOwner = new User
        {
            DisplayName = "Platform Owner",
            Email = "platform.owner@example.test",
            Role = UserRole.Admin
        };
        var prompt = new Prompt
        {
            ProjectId = project.Id,
            AuthorUserId = owner.Id,
            Title = "Secret",
            Content = "Use {{api_token}}."
        };
        var definition = new VariableDefinition
        {
            UserId = owner.Id,
            Scope = VariableScope.User,
            Name = "api_token",
            IsSecret = true
        };
        database.DbContext.Users.Add(platformOwner);
        database.DbContext.Prompts.Add(prompt);
        database.DbContext.VariableDefinitions.Add(definition);
        database.DbContext.VariableValues.Add(new VariableValue
        {
            VariableDefinitionId = definition.Id,
            UserId = owner.Id,
            Value = protector.Protect("owner-secret-token"),
            IsEncrypted = true
        });
        await database.SaveChangesAsync();
        var service = CreateService(database, protector);

        var result = await service.ResolveAsync(platformOwner.Id, prompt.Id, new Dictionary<string, string?>());

        Assert.Equal("Use {{api_token}}.", result.ResolvedContent);
        Assert.Equal(["api_token"], result.MissingVariables);
    }

    private static PromptResolutionService CreateService(TestDatabase database, ISecretProtector? protector = null) =>
        new(database.UnitOfWork, new TenantScopeService(database.UnitOfWork), protector ?? new AesSecretProtector(TestConfiguration.Create()));
}
