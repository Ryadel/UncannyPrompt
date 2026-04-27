using Serilog;
using UncannyPrompt.Infrastructure.Configuration;

namespace UncannyPrompt.WebApp.Extensions;

public static class WebApplicationBuilderExtensions
{
    public static WebApplicationBuilder ConfigureUncannyPromptHost(this WebApplicationBuilder builder)
    {
        UncannyPromptEnvironmentFileLoader.Load(builder.Environment.ContentRootPath);
        builder.Configuration.AddEnvironmentVariables();
        UncannyPromptConfigurationComposer.ComposeDerivedValues(builder.Configuration);

        builder.Host.UseSerilog((context, logger) =>
        {
            logger.ReadFrom.Configuration(context.Configuration)
                .Enrich.FromLogContext()
                .WriteTo.Console();
        });

        return builder;
    }
}
