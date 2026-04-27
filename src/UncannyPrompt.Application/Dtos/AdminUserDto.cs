using UncannyPrompt.Domain;

namespace UncannyPrompt.Application;

public sealed record AdminUserDto(
    Guid Id,
    string DisplayName,
    string Email,
    UserRole Role,
    UserStatus Status,
    string? LastLoginProvider,
    DateTimeOffset? LastLoginAt,
    DateTimeOffset CreatedAt);
