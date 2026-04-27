using UncannyPrompt.Domain;

namespace UncannyPrompt.Application;

public sealed record WorkspaceDto(Guid Id, Guid TenantId, string Name, string? Description);
