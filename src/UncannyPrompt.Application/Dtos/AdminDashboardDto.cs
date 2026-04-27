namespace UncannyPrompt.Application;

public sealed record AdminDashboardDto(
    int UserCount,
    int ActiveUserCount,
    int TenantCount,
    int WorkspaceCount,
    int ProjectCount,
    int PromptCount,
    IReadOnlyList<AdminUserDto> RecentUsers);
