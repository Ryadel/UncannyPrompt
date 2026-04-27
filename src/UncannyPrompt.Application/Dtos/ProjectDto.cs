using UncannyPrompt.Domain;

namespace UncannyPrompt.Application;

public sealed record ProjectDto(Guid Id, Guid WorkspaceId, string Name, string? Description, DateTimeOffset CreatedAt, DateTimeOffset? UpdatedAt);
