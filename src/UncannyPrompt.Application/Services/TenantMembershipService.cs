using Microsoft.EntityFrameworkCore;
using UncannyPrompt.Domain;

namespace UncannyPrompt.Application;

internal sealed class TenantMembershipService(IUnitOfWork unitOfWork) : ITenantMembershipService
{
    public async Task<IReadOnlyList<TenantMemberDto>> GetMembersAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var memberships = await unitOfWork.Repository<TenantMembership>().Query()
            .Where(x => x.TenantId == tenantId)
            .Join(
                unitOfWork.Repository<User>().Query(),
                membership => membership.UserId,
                user => user.Id,
                (membership, user) => new { membership, user })
            .GroupJoin(
                unitOfWork.Repository<User>().Query(),
                joined => joined.membership.GrantedByUserId,
                grantedByUser => (Guid?)grantedByUser.Id,
                (joined, grantedByUsers) => new { joined.membership, joined.user, grantedByUsers })
            .SelectMany(
                x => x.grantedByUsers.DefaultIfEmpty(),
                (x, grantedByUser) => new
                {
                    x.membership,
                    x.user,
                    GrantedByEmail = grantedByUser == null ? null : grantedByUser.Email
                })
            .OrderBy(x => x.membership.Role)
            .ThenBy(x => x.user.Email)
            .ToListAsync(cancellationToken);

        return memberships
            .Select(x => new TenantMemberDto(
                x.user.Id,
                x.user.Email,
                x.user.DisplayName,
                x.user.Status,
                x.membership.Role,
                x.membership.CanGrant,
                x.membership.GrantedByUserId,
                x.GrantedByEmail,
                x.membership.GrantedAt))
            .ToList();
    }

    public async Task SaveAsync(
        Guid actorUserId,
        Guid tenantId,
        SaveTenantMembershipRequest request,
        bool isPlatformAdmin,
        CancellationToken cancellationToken = default)
    {
        var actorMembership = await GetActorMembershipAsync(actorUserId, tenantId, isPlatformAdmin, cancellationToken);

        if (!isPlatformAdmin)
        {
            ValidateCanManageMembers(actorMembership!);
            ValidateCanAssignRole(actorMembership!.Role, request.Role);
        }

        var targetUser = await unitOfWork.Repository<User>().FindAsync(request.UserId, cancellationToken)
            ?? throw new InvalidOperationException("Target user not found.");
        if (targetUser.Status != UserStatus.Active)
        {
            throw new InvalidOperationException("Cannot assign permissions to an inactive user.");
        }

        var tenant = await unitOfWork.Repository<Tenant>().FindAsync(tenantId, cancellationToken)
            ?? throw new InvalidOperationException("Tenant not found.");
        if (tenant.IsDeleted)
        {
            throw new InvalidOperationException("Tenant is deleted.");
        }

        if (tenant.Kind == TenantKind.Personal &&
            tenant.OwnerUserId == request.UserId &&
            request.Role != TenantRole.TenantOwner)
        {
            throw new InvalidOperationException("You cannot change the owner role on a personal tenant.");
        }

        var existing = await unitOfWork.Repository<TenantMembership>().Query()
            .FirstOrDefaultAsync(x => x.UserId == request.UserId && x.TenantId == tenantId, cancellationToken);

        if (!isPlatformAdmin && existing is not null && existing.Role < actorMembership!.Role)
        {
            throw new InvalidOperationException("You cannot modify a member with higher privileges.");
        }

        if (existing is not null &&
            existing.Role == TenantRole.TenantOwner &&
            request.Role != TenantRole.TenantOwner)
        {
            var ownerCount = await CountMembersByRoleAsync(tenantId, TenantRole.TenantOwner, cancellationToken);
            if (ownerCount <= 1)
            {
                throw new InvalidOperationException("At least one Tenant Owner is required.");
            }
        }

        var canGrant = CanGrantForRole(request.Role);
        var now = DateTimeOffset.UtcNow;

        if (existing is null)
        {
            await unitOfWork.Repository<TenantMembership>().AddAsync(new TenantMembership
            {
                TenantId = tenantId,
                UserId = request.UserId,
                Role = request.Role,
                IsActive = true,
                CanGrant = canGrant,
                GrantedByUserId = actorUserId,
                GrantedAt = now
            }, cancellationToken);
        }
        else
        {
            existing.Role = request.Role;
            existing.CanGrant = canGrant;
            existing.GrantedByUserId = actorUserId;
            existing.GrantedAt = now;
            existing.IsActive = true;
            existing.UpdatedAt = now;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task RevokeAsync(
        Guid actorUserId,
        Guid tenantId,
        Guid targetUserId,
        bool isPlatformAdmin,
        CancellationToken cancellationToken = default)
    {
        var actorMembership = await GetActorMembershipAsync(actorUserId, tenantId, isPlatformAdmin, cancellationToken);

        var targetMembership = await unitOfWork.Repository<TenantMembership>().Query()
            .FirstOrDefaultAsync(x => x.UserId == targetUserId && x.TenantId == tenantId, cancellationToken);
        if (targetMembership is null)
        {
            return;
        }

        if (!isPlatformAdmin)
        {
            ValidateCanManageMembers(actorMembership!);
            if (targetMembership.Role < actorMembership!.Role)
            {
                throw new InvalidOperationException("You cannot revoke a member with higher privileges.");
            }
        }

        var tenant = await unitOfWork.Repository<Tenant>().FindAsync(tenantId, cancellationToken)
            ?? throw new InvalidOperationException("Tenant not found.");

        if (tenant.Kind == TenantKind.Personal && tenant.OwnerUserId == targetUserId)
        {
            throw new InvalidOperationException("You cannot remove the owner of a personal tenant.");
        }

        if (targetMembership.Role == TenantRole.TenantOwner)
        {
            var ownerCount = await CountMembersByRoleAsync(tenantId, TenantRole.TenantOwner, cancellationToken);
            if (ownerCount <= 1)
            {
                throw new InvalidOperationException("At least one Tenant Owner is required.");
            }
        }

        var visited = new HashSet<Guid>();
        await RevokeRecursiveAsync(tenantId, targetUserId, visited, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<TenantMembership?> GetActorMembershipAsync(Guid actorUserId, Guid tenantId, bool isPlatformAdmin, CancellationToken cancellationToken)
    {
        if (isPlatformAdmin)
        {
            return null;
        }

        var membership = await unitOfWork.Repository<TenantMembership>().Query()
            .FirstOrDefaultAsync(x => x.UserId == actorUserId && x.TenantId == tenantId && x.IsActive, cancellationToken);
        if (membership is null)
        {
            throw new InvalidOperationException("You are not a member of the current tenant.");
        }

        return membership;
    }

    private Task<int> CountMembersByRoleAsync(Guid tenantId, TenantRole role, CancellationToken cancellationToken) =>
        unitOfWork.Repository<TenantMembership>().Query()
            .CountAsync(x => x.TenantId == tenantId && x.Role == role && x.IsActive, cancellationToken);

    private async Task RevokeRecursiveAsync(Guid tenantId, Guid userId, HashSet<Guid> visited, CancellationToken cancellationToken)
    {
        if (!visited.Add(userId))
        {
            return;
        }

        var delegated = await unitOfWork.Repository<TenantMembership>().Query()
            .Where(x => x.TenantId == tenantId && x.GrantedByUserId == userId)
            .Select(x => x.UserId)
            .ToListAsync(cancellationToken);

        foreach (var childUserId in delegated)
        {
            await RevokeRecursiveAsync(tenantId, childUserId, visited, cancellationToken);
        }

        var membership = await unitOfWork.Repository<TenantMembership>().Query()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.UserId == userId, cancellationToken);
        if (membership is not null)
        {
            unitOfWork.Repository<TenantMembership>().Remove(membership);
        }
    }

    private static void ValidateCanManageMembers(TenantMembership membership)
    {
        var canManage = membership.Role <= TenantRole.TenantManager && membership.CanGrant;
        if (!canManage)
        {
            throw new InvalidOperationException("You do not have permission to manage tenant members.");
        }
    }

    private static void ValidateCanAssignRole(TenantRole actorRole, TenantRole targetRole)
    {
        if (targetRole < actorRole)
        {
            throw new InvalidOperationException("You cannot assign a role higher than your own.");
        }
    }

    private static bool CanGrantForRole(TenantRole role)
        => role is TenantRole.TenantOwner or TenantRole.TenantManager;
}
