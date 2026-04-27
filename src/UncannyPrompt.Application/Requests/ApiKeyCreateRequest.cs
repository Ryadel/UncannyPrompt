namespace UncannyPrompt.Application;

public sealed record ApiKeyCreateRequest(string Name, DateTimeOffset? ExpiresAt);
