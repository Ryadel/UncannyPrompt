namespace UncannyPrompt.WebApp.Controllers;

public sealed record PublicLinkCreateRequest(Guid PromptId, DateTimeOffset? ExpiresAt);
