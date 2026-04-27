using UncannyPrompt.Domain;

namespace UncannyPrompt.Application;

public sealed record FolderDto(Guid Id, Guid ProjectId, Guid? ParentFolderId, string Name);
