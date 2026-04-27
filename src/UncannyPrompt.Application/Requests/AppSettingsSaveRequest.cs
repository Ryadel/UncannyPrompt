namespace UncannyPrompt.Application;

public sealed record AppSettingsSaveRequest(
    Guid? TenantId,
    bool PublicSharingEnabled,
    bool PromptCopyTrackingEnabled,
    bool OnboardingEnabled,
    int PromptVersionHistoryLimit,
    int MaxPromptsPerProject);
