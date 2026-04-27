namespace UncannyPrompt.Application;

public interface IAdminService
{
    Task<AdminDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AdminUserDto>> ListUsersAsync(CancellationToken cancellationToken = default);
    Task<AdminUserDto?> GetUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task SaveUserAsync(Guid actorUserId, AdminUserSaveRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AdminTenantDto>> ListTenantsAsync(CancellationToken cancellationToken = default);
}
