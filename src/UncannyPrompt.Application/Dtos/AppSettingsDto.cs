namespace UncannyPrompt.Application;

public sealed record AppSettingsDto(
    Guid? TenantId,
    string ScopeName,
    bool PublicSharingEnabled,
    bool PromptCopyTrackingEnabled,
    bool OnboardingEnabled,
    int PromptVersionHistoryLimit,
    int MaxPromptsPerProject,
    DateTimeOffset? UpdatedAt);
