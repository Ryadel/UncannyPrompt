using Microsoft.EntityFrameworkCore;
using UncannyPrompt.Domain;

namespace UncannyPrompt.Application;

internal sealed class AdminService(IUnitOfWork unitOfWork, IAuditService auditService) : IAdminService
{
    public async Task<AdminDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var users = unitOfWork.Repository<User>().Query();
        var tenants = unitOfWork.Repository<Tenant>().Query().Where(x => !x.IsDeleted);
        var workspaces = unitOfWork.Repository<Workspace>().Query().Where(x => !x.IsDeleted);
        var projects = unitOfWork.Repository<Project>().Query().Where(x => !x.IsDeleted);
        var prompts = unitOfWork.Repository<Prompt>().Query().Where(x => !x.IsDeleted);

        var recentUsers = await users
            .OrderByDescending(x => x.CreatedAt)
            .Take(5)
            .Select(x => new AdminUserDto(x.Id, x.DisplayName, x.Email, x.Role, x.Status, x.LastLoginProvider, x.LastLoginAt, x.CreatedAt))
            .ToListAsync(cancellationToken);

        return new AdminDashboardDto(
            await users.CountAsync(cancellationToken),
            await users.CountAsync(x => x.Status == UserStatus.Active, cancellationToken),
            await tenants.CountAsync(cancellationToken),
            await workspaces.CountAsync(cancellationToken),
            await projects.CountAsync(cancellationToken),
            await prompts.CountAsync(cancellationToken),
            recentUsers);
    }

    public async Task<IReadOnlyList<AdminUserDto>> ListUsersAsync(CancellationToken cancellationToken = default) =>
        await unitOfWork.Repository<User>().Query()
            .OrderBy(x => x.DisplayName)
            .ThenBy(x => x.Email)
            .Select(x => new AdminUserDto(x.Id, x.DisplayName, x.Email, x.Role, x.Status, x.LastLoginProvider, x.LastLoginAt, x.CreatedAt))
            .ToListAsync(cancellationToken);

    public async Task<AdminUserDto?> GetUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await unitOfWork.Repository<User>().Query()
            .Where(x => x.Id == userId)
            .Select(x => new AdminUserDto(x.Id, x.DisplayName, x.Email, x.Role, x.Status, x.LastLoginProvider, x.LastLoginAt, x.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task SaveUserAsync(Guid actorUserId, AdminUserSaveRequest request, CancellationToken cancellationToken = default)
    {
        var user = await unitOfWork.Repository<User>().FindAsync(request.UserId, cancellationToken)
            ?? throw new InvalidOperationException("User was not found.");

        if (user.Role == UserRole.Admin &&
            (request.Role != UserRole.Admin || request.Status != UserStatus.Active))
        {
            var activeAdminCount = await unitOfWork.Repository<User>().Query()
                .CountAsync(x => x.Role == UserRole.Admin && x.Status == UserStatus.Active, cancellationToken);

            if (activeAdminCount <= 1)
            {
                throw new InvalidOperationException("At least one active admin is required.");
            }
        }

        user.Role = request.Role;
        user.Status = request.Status;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await auditService.RecordAsync(actorUserId, "admin.user.updated", "User", user.Id, data: new { user.Role, user.Status }, cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<AdminTenantDto>> ListTenantsAsync(CancellationToken cancellationToken = default)
    {
        var tenants = unitOfWork.Repository<Tenant>().Query().Where(x => !x.IsDeleted);
        var memberships = unitOfWork.Repository<TenantMembership>().Query();
        var workspaces = unitOfWork.Repository<Workspace>().Query().Where(x => !x.IsDeleted);

        return await tenants
            .OrderBy(x => x.Name)
            .Select(x => new AdminTenantDto(
                x.Id,
                x.Name,
                x.Kind,
                memberships.Count(m => m.TenantId == x.Id && m.IsActive),
                workspaces.Count(w => w.TenantId == x.Id),
                x.CreatedAt))
            .ToListAsync(cancellationToken);
    }
}
