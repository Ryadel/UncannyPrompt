using UncannyPrompt.Domain;

namespace UncannyPrompt.Application;

public sealed record TenantMemberDto(
    Guid UserId,
    string Email,
    string DisplayName,
    UserStatus Status,
    TenantRole Role,
    bool CanGrant,
    Guid? GrantedByUserId,
    string? GrantedByEmail,
    DateTimeOffset? GrantedAt);
