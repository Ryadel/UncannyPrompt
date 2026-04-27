using Microsoft.EntityFrameworkCore;
using UncannyPrompt.Domain;
using UncannyPrompt.Infrastructure;

namespace UncannyPrompt.WebApp.Extensions;

public static class WebApplicationSeedExtensions
{
    public static async Task InitializeUncannyPromptDatabaseAndSeedAsync(this WebApplication app, IConfiguration configuration)
    {
        var applyMigrationsOnStartup = configuration.GetValue<bool?>("Database:ApplyMigrationsOnStartup")
            ?? app.Environment.IsDevelopment();

        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<UncannyPromptDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Startup.Database");

        if (applyMigrationsOnStartup)
        {
            var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync(CancellationToken.None);
            var appliedMigrations = await dbContext.Database.GetAppliedMigrationsAsync(CancellationToken.None);

            logger.LogInformation("EF Provider: {Provider}", dbContext.Database.ProviderName);
            logger.LogInformation("Applied migrations ({Count}): {Migrations}", appliedMigrations.Count(), string.Join(", ", appliedMigrations));
            logger.LogInformation("Pending migrations ({Count}): {Migrations}", pendingMigrations.Count(), string.Join(", ", pendingMigrations));

            await dbContext.Database.MigrateAsync(CancellationToken.None);
        }
        else
        {
            logger.LogInformation("Database migrations on startup are disabled.");
        }

        var seedAdmins = GetSeedAdminUsers(configuration);
        if (seedAdmins.Count == 0)
        {
            return;
        }

        var hasChanges = false;
        foreach (var seedAdmin in seedAdmins)
        {
            hasChanges |= await SeedAdminUserAsync(dbContext, logger, seedAdmin, CancellationToken.None);
        }

        if (hasChanges)
        {
            await dbContext.SaveChangesAsync(CancellationToken.None);
        }
    }

    private static IReadOnlyList<SeedAdminUserOptions> GetSeedAdminUsers(IConfiguration configuration) =>
        configuration
            .GetSection("SeedOptions:AdminUsers")
            .GetChildren()
            .Select(section => new SeedAdminUserOptions
            {
                Provider = section["Provider"],
                UniqueId = section["UniqueId"],
                Email = section["Email"]
            })
            .Where(seedAdmin => !seedAdmin.IsEmpty)
            .ToList();

    private static async Task<bool> SeedAdminUserAsync(
        UncannyPromptDbContext dbContext,
        ILogger logger,
        SeedAdminUserOptions seedAdmin,
        CancellationToken cancellationToken)
    {
        var provider = NormalizeProvider(seedAdmin.Provider);
        var uniqueId = seedAdmin.UniqueId?.Trim();
        var email = seedAdmin.Email?.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(provider) ||
            string.IsNullOrWhiteSpace(uniqueId) ||
            string.IsNullOrWhiteSpace(email))
        {
            logger.LogWarning("Skipping incomplete seed admin entry. Provider, UniqueId, and Email are required.");
            return false;
        }

        var identity = await dbContext.AuthIdentities
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Provider == provider && x.ProviderSubjectId == uniqueId, cancellationToken);
        var emailUser = await dbContext.Users.FirstOrDefaultAsync(x => x.Email == email, cancellationToken);
        if (identity?.User is { } identityUser && emailUser is not null && emailUser.Id != identityUser.Id)
        {
            logger.LogWarning("Skipping seed admin {Email}: provider identity {Provider}/{UniqueId} is linked to a different user.", email, provider, uniqueId);
            return false;
        }

        var user = identity?.User ?? emailUser;
        var hasChanges = false;

        if (user is null)
        {
            user = new User
            {
                Email = email,
                DisplayName = email,
                Role = UserRole.Admin,
                Status = UserStatus.Active
            };
            await dbContext.Users.AddAsync(user, cancellationToken);
            hasChanges = true;
            logger.LogInformation("Seeded platform admin user {Email} for provider {Provider}.", email, provider);
        }

        if (identity is null)
        {
            await dbContext.AuthIdentities.AddAsync(new AuthIdentity
            {
                UserId = user.Id,
                Provider = provider,
                ProviderSubjectId = uniqueId,
                Email = email,
                DisplayName = user.DisplayName
            }, cancellationToken);
            hasChanges = true;
        }
        else if (identity.UserId != user.Id)
        {
            logger.LogWarning("Skipping seed admin {Email}: provider identity {Provider}/{UniqueId} is already linked to another user.", email, provider, uniqueId);
            return hasChanges;
        }

        if (!string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase))
        {
            user.Email = email;
            hasChanges = true;
        }

        if (string.IsNullOrWhiteSpace(user.DisplayName))
        {
            user.DisplayName = email;
            hasChanges = true;
        }

        if (user.Role != UserRole.Admin)
        {
            user.Role = UserRole.Admin;
            hasChanges = true;
        }

        if (user.Status != UserStatus.Active)
        {
            user.Status = UserStatus.Active;
            hasChanges = true;
        }

        if (identity is not null)
        {
            if (!string.Equals(identity.Email, email, StringComparison.OrdinalIgnoreCase))
            {
                identity.Email = email;
                hasChanges = true;
            }

            if (string.IsNullOrWhiteSpace(identity.DisplayName))
            {
                identity.DisplayName = user.DisplayName;
                hasChanges = true;
            }
        }

        if (hasChanges)
        {
            user.UpdatedAt = DateTimeOffset.UtcNow;
            logger.LogInformation("Ensured platform admin user {Email} for provider {Provider}.", email, provider);
        }

        return hasChanges;
    }

    private static string? NormalizeProvider(string? provider)
    {
        var normalized = provider?.Trim().Replace(" ", string.Empty, StringComparison.OrdinalIgnoreCase).ToLowerInvariant();
        return normalized switch
        {
            "entraid" or "microsoftentraid" or "azuread" or "azureactivedirectory" or "openidconnect" or "entra" =>
                "entra",
            "google" => "google",
            "github" => "github",
            "development" => "development",
            _ => null
        };
    }

    private sealed class SeedAdminUserOptions
    {
        public string? Provider { get; init; }
        public string? UniqueId { get; init; }
        public string? Email { get; init; }

        public bool IsEmpty =>
            string.IsNullOrWhiteSpace(Provider) &&
            string.IsNullOrWhiteSpace(UniqueId) &&
            string.IsNullOrWhiteSpace(Email);
    }
}
