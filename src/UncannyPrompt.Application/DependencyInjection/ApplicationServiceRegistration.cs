using Microsoft.Extensions.DependencyInjection;

namespace UncannyPrompt.Application;

public static class ApplicationServiceRegistration
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IPermissionService, PermissionService>();
        services.AddScoped<ITenantScopeService, TenantScopeService>();
        services.AddScoped<ITenantService, TenantService>();
        services.AddScoped<ITenantMembershipService, TenantMembershipService>();
        services.AddScoped<IWorkspaceService, WorkspaceService>();
        services.AddScoped<IProjectService, ProjectService>();
        services.AddScoped<IFolderService, FolderService>();
        services.AddScoped<IPromptService, PromptService>();
        services.AddScoped<IPromptVersionService, PromptVersionService>();
        services.AddScoped<ITagService, TagService>();
        services.AddScoped<IVariableService, VariableService>();
        services.AddScoped<IPromptResolutionService, PromptResolutionService>();
        services.AddScoped<ISharingService, SharingService>();
        services.AddScoped<ISearchService, SearchService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IAdminService, AdminService>();
        services.AddScoped<IAppSettingsService, AppSettingsService>();
        services.AddScoped<IUserApiKeyService, UserApiKeyService>();
        return services;
    }
}
