using UncannyPrompt.Domain;

namespace UncannyPrompt.Application;

public sealed record TenantDto(Guid Id, string Name, TenantKind Kind, TenantRole Role);
