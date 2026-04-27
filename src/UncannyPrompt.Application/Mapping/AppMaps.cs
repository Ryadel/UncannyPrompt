using UncannyPrompt.Domain;

namespace UncannyPrompt.Application;

internal static class AppMaps
{
    public static UserDto ToDto(this User user) => new(user.Id, user.DisplayName, user.Email, user.Role, user.Status, user.OnboardingDismissedAt);

    public static TenantDto ToDto(this Tenant tenant, TenantRole role) => new(tenant.Id, tenant.Name, tenant.Kind, role);

    public static WorkspaceDto ToDto(this Workspace workspace) => new(workspace.Id, workspace.TenantId, workspace.Name, workspace.Description);

    public static ProjectDto ToDto(this Project project) => new(project.Id, project.WorkspaceId, project.Name, project.Description, project.CreatedAt, project.UpdatedAt);

    public static FolderDto ToDto(this Folder folder) => new(folder.Id, folder.ProjectId, folder.ParentFolderId, folder.Name);

    public static PromptVersionDto ToDto(this PromptVersion version) =>
        new(version.Id, version.PromptId, version.VersionNumber, version.Content, version.Label, version.Changelog, version.Notes, version.CreatedAt);

    public static AuditEventDto ToDto(this AuditEvent auditEvent) =>
        new(auditEvent.Id, auditEvent.EventType, auditEvent.EntityType, auditEvent.EntityId, auditEvent.CreatedAt, auditEvent.DataJson);

    public static TagDto ToDto(this Tag tag) => new(tag.Id, tag.Name, tag.Scope);

    public static ShareGrantDto ToDto(this ShareGrant grant) =>
        new(grant.Id, grant.TargetType, grant.TargetId, grant.UserId, grant.TenantId, grant.WorkspaceId, grant.Permission);

    public static PromptDto ToDto(this Prompt prompt, Guid userId, IUnitOfWork unitOfWork)
    {
        var tags =
            from promptTag in unitOfWork.Repository<PromptTag>().Query()
            join tag in unitOfWork.Repository<Tag>().Query() on promptTag.TagId equals tag.Id
            where promptTag.PromptId == prompt.Id
            orderby tag.Name
            select tag.Name;

        var isFavorite = userId != Guid.Empty && unitOfWork.Repository<Favorite>().Query().Any(x => x.PromptId == prompt.Id && x.UserId == userId);

        return new PromptDto(
            prompt.Id,
            prompt.ProjectId,
            prompt.FolderId,
            prompt.Title,
            prompt.Summary,
            prompt.Content,
            prompt.Notes,
            prompt.Status,
            prompt.CurrentVersionNumber,
            isFavorite,
            prompt.IsPinned,
            prompt.CopyCount,
            prompt.LastUsedAt,
            tags.Distinct().ToList(),
            prompt.CreatedAt,
            prompt.UpdatedAt);
    }
}
