namespace UncannyPrompt.WebApp.Controllers;

public sealed record FolderUpsertApiRequest(Guid ProjectId, Guid? ParentFolderId, string Name);
