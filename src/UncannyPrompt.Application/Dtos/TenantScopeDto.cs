using UncannyPrompt.Domain;

namespace UncannyPrompt.Application;

public sealed record TenantScopeDto(Guid TenantId, string TenantName, Guid WorkspaceId, string WorkspaceName, Guid ProjectId, string ProjectName);
