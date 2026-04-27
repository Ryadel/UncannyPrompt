using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using UncannyPrompt.Application;

namespace UncannyPrompt.Infrastructure;

public static class InfrastructureRegistration
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("UncannyPrompt")
            ?? configuration["ConnectionStrings:UncannyPrompt"]
            ?? throw new InvalidOperationException("Missing ConnectionStrings:UncannyPrompt. Configure Database:* values or provide the connection string directly.");

        services.AddDbContext<UncannyPromptDbContext>(options =>
            options.UseSqlServer(connectionString, sql => sql.MigrationsAssembly(typeof(UncannyPromptDbContext).Assembly.FullName)));

        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddSingleton<ISecretProtector, AesSecretProtector>();
        services.AddSingleton<IApiKeyHasher, ApiKeyHasher>();
        services.AddSingleton<IPublicTokenHasher, PublicTokenHasher>();
        return services;
    }
}
