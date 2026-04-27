using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace UncannyPrompt.Infrastructure.Configuration;

public static class UncannyPromptConfigurationComposer
{
    public static void ComposeDerivedValues(IConfigurationManager configuration)
    {
        ComposeDatabaseConnectionString(configuration);
        ComposeEntraIdAuthority(configuration);
    }

    private static void ComposeDatabaseConnectionString(IConfigurationManager configuration)
    {
        if (!string.IsNullOrWhiteSpace(configuration.GetConnectionString("UncannyPrompt")))
        {
            return;
        }

        var host = configuration["Database:Host"];
        var name = configuration["Database:Name"];
        var username = configuration["Database:Username"];

        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(username))
        {
            return;
        }

        var port = int.TryParse(configuration["Database:Port"], out var parsedPort)
            ? parsedPort
            : 0;
        var trustServerCertificate = !bool.TryParse(configuration["Database:TrustServerCertificate"], out var parsedTrust)
            || parsedTrust;

        var builder = new SqlConnectionStringBuilder
        {
            DataSource = port > 0 ? $"{host},{port}" : host,
            InitialCatalog = name,
            UserID = username,
            Password = configuration["Database:Password"] ?? string.Empty,
            TrustServerCertificate = trustServerCertificate
        };

        configuration["ConnectionStrings:UncannyPrompt"] = builder.ConnectionString;
    }

    private static void ComposeEntraIdAuthority(IConfigurationManager configuration)
    {
        if (!string.IsNullOrWhiteSpace(configuration["Authentication:EntraId:Authority"]))
        {
            return;
        }

        var instance = configuration["Authentication:EntraId:Instance"];
        var tenantId = configuration["Authentication:EntraId:TenantId"];

        if (string.IsNullOrWhiteSpace(instance) || string.IsNullOrWhiteSpace(tenantId))
        {
            return;
        }

        configuration["Authentication:EntraId:Authority"] = $"{instance.TrimEnd('/')}/{tenantId}";
    }
}
