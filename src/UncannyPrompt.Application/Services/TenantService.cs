using Microsoft.EntityFrameworkCore;
using UncannyPrompt.Domain;
using UncannyPrompt.Shared;

namespace UncannyPrompt.Application;

internal sealed class TenantService(IUnitOfWork unitOfWork) : ITenantService
{
    public async Task<IReadOnlyList<TenantDto>> ListAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var tenants =
            from membership in unitOfWork.Repository<TenantMembership>().Query()
            join tenant in unitOfWork.Repository<Tenant>().Query() on membership.TenantId equals tenant.Id
            where membership.UserId == userId && membership.IsActive && !tenant.IsDeleted
            orderby tenant.Kind, tenant.Name
            select tenant.ToDto(membership.Role);

        return await tenants.Take(100).ToListAsync(cancellationToken);
    }

    public async Task<TenantDto> CreateCollaborativeAsync(Guid ownerUserId, string name, CancellationToken cancellationToken = default)
    {
        var tenant = new Tenant { Name = name.Trim(), Kind = TenantKind.Collaborative, OwnerUserId = ownerUserId };
        await unitOfWork.Repository<Tenant>().AddAsync(tenant, cancellationToken);
        await unitOfWork.Repository<TenantMembership>().AddAsync(new TenantMembership
        {
            TenantId = tenant.Id,
            UserId = ownerUserId,
            Role = TenantRole.TenantOwner,
            CanGrant = true
        }, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return tenant.ToDto(TenantRole.TenantOwner);
    }
}
