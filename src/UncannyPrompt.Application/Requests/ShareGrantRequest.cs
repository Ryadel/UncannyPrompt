using UncannyPrompt.Domain;

namespace UncannyPrompt.Application;

public sealed record ShareGrantRequest(ShareTargetType TargetType, Guid TargetId, Guid? UserId, Guid? TenantId, Guid? WorkspaceId, SharePermission Permission);
