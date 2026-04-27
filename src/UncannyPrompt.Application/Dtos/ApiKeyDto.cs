namespace UncannyPrompt.Application;

public sealed record ApiKeyDto(
    Guid Id,
    string Name,
    string Prefix,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ExpiresAt,
    DateTimeOffset? LastUsedAt);

public sealed record ApiKeyCreateResultDto(ApiKeyDto ApiKey, string PlainTextKey);
