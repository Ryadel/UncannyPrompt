using UncannyPrompt.Domain;

namespace UncannyPrompt.Application;

public sealed record AdminTenantDto(
    Guid Id,
    string Name,
    TenantKind Kind,
    int MemberCount,
    int WorkspaceCount,
    DateTimeOffset CreatedAt);
