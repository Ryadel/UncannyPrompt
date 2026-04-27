namespace UncannyPrompt.Application;

public interface IAppSettingsService
{
    Task<AppSettingsDto> GetAsync(Guid? tenantId = null, CancellationToken cancellationToken = default);
    Task<AppSettingsDto> SaveAsync(Guid actorUserId, AppSettingsSaveRequest request, CancellationToken cancellationToken = default);
}
