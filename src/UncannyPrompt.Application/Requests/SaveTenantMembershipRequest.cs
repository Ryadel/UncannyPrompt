using UncannyPrompt.Domain;

namespace UncannyPrompt.Application;

public sealed record SaveTenantMembershipRequest(Guid UserId, TenantRole Role);
