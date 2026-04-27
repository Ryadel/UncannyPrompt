using UncannyPrompt.Domain;

namespace UncannyPrompt.Application;

public sealed record TagDto(Guid Id, string Name, TagScope Scope);
