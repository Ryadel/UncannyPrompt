using Microsoft.EntityFrameworkCore;
using UncannyPrompt.Domain;

namespace UncannyPrompt.Application;

internal sealed class AppSettingsService(IUnitOfWork unitOfWork, IAuditService auditService) : IAppSettingsService
{
    private const string PublicSharingEnabledKey = "PublicSharingEnabled";
    private const string PromptCopyTrackingEnabledKey = "PromptCopyTrackingEnabled";
    private const string OnboardingEnabledKey = "OnboardingEnabled";
    private const string PromptVersionHistoryLimitKey = "PromptVersionHistoryLimit";
    private const string MaxPromptsPerProjectKey = "MaxPromptsPerProject";

    public async Task<AppSettingsDto> GetAsync(Guid? tenantId = null, CancellationToken cancellationToken = default)
    {
        var globalSettings = await LoadValuesAsync(AppSettingScope.Global, null, cancellationToken);
        var tenantSettings = tenantId is Guid id
            ? await LoadValuesAsync(AppSettingScope.Tenant, id, cancellationToken)
            : [];

        var values = Defaults();
        Overlay(values, globalSettings);
        Overlay(values, tenantSettings);

        var scopeName = tenantId is Guid requestedTenantId
            ? await unitOfWork.Repository<Tenant>().Query()
                .Where(x => x.Id == requestedTenantId && !x.IsDeleted)
                .Select(x => x.Name)
                .FirstOrDefaultAsync(cancellationToken) ?? "Tenant"
            : "Global";

        return ToDto(tenantId, scopeName, values, LatestUpdatedAt(globalSettings, tenantSettings));
    }

    public async Task<AppSettingsDto> SaveAsync(Guid actorUserId, AppSettingsSaveRequest request, CancellationToken cancellationToken = default)
    {
        if (request.TenantId is Guid tenantId)
        {
            var tenantExists = await unitOfWork.Repository<Tenant>().Query()
                .AnyAsync(x => x.Id == tenantId && !x.IsDeleted, cancellationToken);
            if (!tenantExists)
            {
                throw new InvalidOperationException("Tenant was not found.");
            }
        }

        var values = Defaults();
        values[PublicSharingEnabledKey] = request.PublicSharingEnabled.ToString();
        values[PromptCopyTrackingEnabledKey] = request.PromptCopyTrackingEnabled.ToString();
        values[OnboardingEnabledKey] = request.OnboardingEnabled.ToString();
        values[PromptVersionHistoryLimitKey] = Clamp(request.PromptVersionHistoryLimit, 1, 200).ToString();
        values[MaxPromptsPerProjectKey] = Clamp(request.MaxPromptsPerProject, 0, 10000).ToString();

        var scope = request.TenantId is null ? AppSettingScope.Global : AppSettingScope.Tenant;
        foreach (var (key, value) in values)
        {
            await UpsertAsync(scope, request.TenantId, key, value, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await auditService.RecordAsync(actorUserId, "admin.settings.updated", "AppSetting", tenantId: request.TenantId, data: new { request.TenantId }, cancellationToken: cancellationToken);
        return await GetAsync(request.TenantId, cancellationToken);
    }

    private async Task UpsertAsync(AppSettingScope scope, Guid? tenantId, string key, string value, CancellationToken cancellationToken)
    {
        var setting = await unitOfWork.Repository<AppSetting>().Query()
            .FirstOrDefaultAsync(x => x.Scope == scope && x.TenantId == tenantId && x.Key == key, cancellationToken);

        if (setting is null)
        {
            await unitOfWork.Repository<AppSetting>().AddAsync(new AppSetting
            {
                Scope = scope,
                TenantId = tenantId,
                Key = key,
                Value = value
            }, cancellationToken);
            return;
        }

        setting.Value = value;
    }

    private async Task<Dictionary<string, AppSetting>> LoadValuesAsync(AppSettingScope scope, Guid? tenantId, CancellationToken cancellationToken) =>
        await unitOfWork.Repository<AppSetting>().Query()
            .Where(x => x.Scope == scope && x.TenantId == tenantId)
            .ToDictionaryAsync(x => x.Key, x => x, cancellationToken);

    private static Dictionary<string, string> Defaults() => new()
    {
        [PublicSharingEnabledKey] = bool.TrueString,
        [PromptCopyTrackingEnabledKey] = bool.TrueString,
        [OnboardingEnabledKey] = bool.TrueString,
        [PromptVersionHistoryLimitKey] = "50",
        [MaxPromptsPerProjectKey] = "0"
    };

    private static void Overlay(Dictionary<string, string> target, Dictionary<string, AppSetting> source)
    {
        foreach (var (key, setting) in source)
        {
            target[key] = setting.Value;
        }
    }

    private static AppSettingsDto ToDto(Guid? tenantId, string scopeName, Dictionary<string, string> values, DateTimeOffset? updatedAt) =>
        new(
            tenantId,
            scopeName,
            ReadBool(values, PublicSharingEnabledKey),
            ReadBool(values, PromptCopyTrackingEnabledKey),
            ReadBool(values, OnboardingEnabledKey),
            Clamp(ReadInt(values, PromptVersionHistoryLimitKey, 50), 1, 200),
            Clamp(ReadInt(values, MaxPromptsPerProjectKey, 0), 0, 10000),
            updatedAt);

    private static bool ReadBool(IReadOnlyDictionary<string, string> values, string key) =>
        values.TryGetValue(key, out var value) && bool.TryParse(value, out var parsed) ? parsed : true;

    private static int ReadInt(IReadOnlyDictionary<string, string> values, string key, int fallback) =>
        values.TryGetValue(key, out var value) && int.TryParse(value, out var parsed) ? parsed : fallback;

    private static DateTimeOffset? LatestUpdatedAt(params Dictionary<string, AppSetting>[] sources)
    {
        var values = sources.SelectMany(x => x.Values)
            .Select(x => x.UpdatedAt ?? x.CreatedAt)
            .OrderByDescending(x => x)
            .ToList();

        return values.Count > 0 ? values[0] : null;
    }

    private static int Clamp(int value, int min, int max) =>
        Math.Min(max, Math.Max(min, value));
}
