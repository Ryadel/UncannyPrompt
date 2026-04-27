namespace UncannyPrompt.Domain;

public enum ApplicationPermission
{
    PromptView = 1,
    PromptCreate = 2,
    PromptEdit = 3,
    PromptDelete = 4,
    PromptRestore = 5,
    PromptShare = 6,
    PromptPublish = 7,
    VersionView = 8,
    VersionRestore = 9,
    FolderCreate = 10,
    FolderEdit = 11,
    FolderDelete = 12,
    ProjectManage = 13,
    TagManage = 14,
    VariableManage = 15,
    TenantManageMembers = 16,
    TenantManageRoles = 17,
    AuditView = 18,
    ExportContent = 19
}
