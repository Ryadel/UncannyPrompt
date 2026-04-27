namespace UncannyPrompt.WebApp.Controllers;

public sealed record WorkspaceCreateRequest(Guid? TenantId, string Name, string? Description);
