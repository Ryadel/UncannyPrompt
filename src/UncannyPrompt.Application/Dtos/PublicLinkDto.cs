using UncannyPrompt.Domain;

namespace UncannyPrompt.Application;

public sealed record PublicLinkDto(Guid Id, Guid PromptId, string Token, DateTimeOffset? ExpiresAt, DateTimeOffset CreatedAt);
