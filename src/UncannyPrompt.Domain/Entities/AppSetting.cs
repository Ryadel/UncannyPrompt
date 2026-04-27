namespace UncannyPrompt.Domain;

public sealed class AppSetting : Entity
{
    public AppSettingScope Scope { get; set; } = AppSettingScope.Global;
    public Guid? TenantId { get; set; }
    public Tenant? Tenant { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}
