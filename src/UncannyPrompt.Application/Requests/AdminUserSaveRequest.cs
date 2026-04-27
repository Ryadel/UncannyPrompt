using UncannyPrompt.Domain;

namespace UncannyPrompt.Application;

public sealed record AdminUserSaveRequest(Guid UserId, UserRole Role, UserStatus Status);
