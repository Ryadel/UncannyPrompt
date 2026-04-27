using UncannyPrompt.Domain;

namespace UncannyPrompt.Application;

public sealed record UserDto(Guid Id, string DisplayName, string Email, UserRole Role, UserStatus Status, DateTimeOffset? OnboardingDismissedAt = null);
