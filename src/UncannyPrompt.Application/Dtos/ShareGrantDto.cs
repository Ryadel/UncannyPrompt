using UncannyPrompt.Domain;

namespace UncannyPrompt.Application;

public sealed record ShareGrantDto(Guid Id, ShareTargetType TargetType, Guid TargetId, Guid? UserId, Guid? TenantId, Guid? WorkspaceId, SharePermission Permission);
