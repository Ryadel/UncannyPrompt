namespace UncannyPrompt.WebApp.Controllers;

public sealed record ProjectCreateRequest(Guid? WorkspaceId, string Name, string? Description);
