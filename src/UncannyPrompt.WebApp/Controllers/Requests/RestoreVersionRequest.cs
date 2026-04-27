namespace UncannyPrompt.WebApp.Controllers;

public sealed record RestoreVersionRequest(Guid VersionId, string? Changelog);
