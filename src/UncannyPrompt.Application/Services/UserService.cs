using UncannyPrompt.Domain;

namespace UncannyPrompt.Application;

internal sealed class UserService(IUnitOfWork unitOfWork, IAuditService auditService) : IUserService
{
    public async Task<UserDto> ProvisionExternalUserAsync(string provider, string subjectId, string? email, string? displayName, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = (email ?? $"{provider}-{subjectId}@local.uncannyprompt").Trim().ToLowerInvariant();
        var now = DateTimeOffset.UtcNow;
        var identity = unitOfWork.Repository<AuthIdentity>().Query().FirstOrDefault(x => x.Provider == provider && x.ProviderSubjectId == subjectId);

        User user;
        if (identity is null)
        {
            user = unitOfWork.Repository<User>().Query().FirstOrDefault(x => x.Email == normalizedEmail)
                ?? new User
                {
                    Email = normalizedEmail,
                    DisplayName = string.IsNullOrWhiteSpace(displayName) ? normalizedEmail : displayName.Trim(),
                    Status = UserStatus.Active
                };

            if (!unitOfWork.Repository<User>().Query().Any(x => x.Id == user.Id))
            {
                await unitOfWork.Repository<User>().AddAsync(user, cancellationToken);
            }

            await unitOfWork.Repository<AuthIdentity>().AddAsync(new AuthIdentity
            {
                UserId = user.Id,
                Provider = provider,
                ProviderSubjectId = subjectId,
                Email = normalizedEmail,
                DisplayName = displayName,
                LastSeenAt = now
            }, cancellationToken);
        }
        else
        {
            user = await unitOfWork.Repository<User>().FindAsync(identity.UserId, cancellationToken)
                ?? throw new InvalidOperationException("Linked user was not found.");
            identity.LastSeenAt = now;
        }

        if (user.Status != UserStatus.Active)
        {
            await auditService.RecordAsync(user.Id, "login.denied.inactive_user", "User", user.Id, data: new { provider }, cancellationToken: cancellationToken);
            throw new UnauthorizedAccessException("User is not active.");
        }

        user.LastLoginAt = now;
        user.LastLoginProvider = provider;
        await EnsurePersonalTenantAsync(user, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await auditService.RecordAsync(user.Id, "login.success", "User", user.Id, data: new { provider }, cancellationToken: cancellationToken);
        return user.ToDto();
    }

    public async Task<UserDto?> GetAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await unitOfWork.Repository<User>().FindAsync(userId, cancellationToken);
        return user?.ToDto();
    }

    private async Task EnsurePersonalTenantAsync(User user, CancellationToken cancellationToken)
    {
        var hasPersonalTenant =
            from membership in unitOfWork.Repository<TenantMembership>().Query()
            join existingTenant in unitOfWork.Repository<Tenant>().Query() on membership.TenantId equals existingTenant.Id
            where membership.UserId == user.Id && existingTenant.Kind == TenantKind.Personal && !existingTenant.IsDeleted
            select existingTenant.Id;

        if (hasPersonalTenant.Any())
        {
            return;
        }

        var tenant = new Tenant { Name = "Personal", Kind = TenantKind.Personal, OwnerUserId = user.Id };

        await unitOfWork.Repository<Tenant>().AddAsync(tenant, cancellationToken);
        await unitOfWork.Repository<TenantMembership>().AddAsync(new TenantMembership { TenantId = tenant.Id, UserId = user.Id, Role = TenantRole.TenantOwner, CanGrant = true }, cancellationToken);
    }
}
