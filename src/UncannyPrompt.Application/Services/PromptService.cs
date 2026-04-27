using Microsoft.EntityFrameworkCore;
using UncannyPrompt.Domain;

namespace UncannyPrompt.Application;

internal sealed class PromptService(IUnitOfWork unitOfWork, IPermissionService permissionService, IAuditService auditService) : IPromptService
{
    public async Task<IReadOnlyList<PromptDto>> ListAsync(Guid userId, PromptSearchRequest request, CancellationToken cancellationToken = default)
    {
        var query = await FilterPromptsAsync(userId, request, cancellationToken);
        var prompts = await query.ToListAsync(cancellationToken);
        return await MapPromptListAsync(userId, prompts, cancellationToken);
    }

    public async Task<IReadOnlyDictionary<Guid, int>> CountByProjectAsync(Guid userId, IReadOnlyCollection<Guid> projectIds, CancellationToken cancellationToken = default)
    {
        if (projectIds.Count == 0)
        {
            return new Dictionary<Guid, int>();
        }

        var accessibleProjectIds = AccessControlQueries.AccessibleProjectIds(unitOfWork, userId, SharePermission.View);
        return await unitOfWork.Repository<Prompt>().Query()
            .Where(x => !x.IsDeleted && projectIds.Contains(x.ProjectId) && accessibleProjectIds.Contains(x.ProjectId))
            .GroupBy(x => x.ProjectId)
            .Select(x => new { ProjectId = x.Key, Count = x.Count() })
            .ToDictionaryAsync(x => x.ProjectId, x => x.Count, cancellationToken);
    }

    public async Task<IReadOnlyDictionary<Guid, int>> CountByWorkspaceAsync(Guid userId, IReadOnlyCollection<Guid> workspaceIds, CancellationToken cancellationToken = default)
    {
        if (workspaceIds.Count == 0)
        {
            return new Dictionary<Guid, int>();
        }

        var accessibleProjectIds = AccessControlQueries.AccessibleProjectIds(unitOfWork, userId, SharePermission.View);
        var counts =
            from prompt in unitOfWork.Repository<Prompt>().Query()
            join project in unitOfWork.Repository<Project>().Query() on prompt.ProjectId equals project.Id
            where !prompt.IsDeleted &&
                  !project.IsDeleted &&
                  workspaceIds.Contains(project.WorkspaceId) &&
                  accessibleProjectIds.Contains(project.Id)
            group prompt by project.WorkspaceId into grouped
            select new { WorkspaceId = grouped.Key, Count = grouped.Count() };

        return await counts.ToDictionaryAsync(x => x.WorkspaceId, x => x.Count, cancellationToken);
    }

    public async Task<PromptDto?> GetAsync(Guid userId, Guid promptId, CancellationToken cancellationToken = default)
    {
        var prompt = await unitOfWork.Repository<Prompt>().FindAsync(promptId, cancellationToken);
        if (prompt is null || prompt.IsDeleted || !await permissionService.CanAccessPromptAsync(userId, prompt.Id, ApplicationPermission.PromptView, cancellationToken))
        {
            return null;
        }

        return prompt.ToDto(userId, unitOfWork);
    }

    public async Task<PromptDto> CreateAsync(Guid userId, PromptUpsertRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureCanWriteTargetAsync(userId, request.ProjectId, request.FolderId, cancellationToken: cancellationToken);

        var prompt = new Prompt
        {
            ProjectId = request.ProjectId,
            FolderId = request.FolderId,
            AuthorUserId = userId,
            Title = request.Title.Trim(),
            Summary = request.Summary,
            Content = request.Content,
            Notes = request.Notes,
            Status = request.Status,
            CurrentVersionNumber = 1
        };

        await unitOfWork.Repository<Prompt>().AddAsync(prompt, cancellationToken);
        await unitOfWork.Repository<PromptVersion>().AddAsync(new PromptVersion
        {
            PromptId = prompt.Id,
            AuthorUserId = userId,
            VersionNumber = 1,
            Content = request.Content,
            Label = request.VersionLabel,
            Changelog = request.Changelog,
            Notes = request.VersionNotes
        }, cancellationToken);
        await SyncTagsAsync(userId, prompt.Id, request.Tags, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await auditService.RecordAsync(userId, "prompt.create", "Prompt", prompt.Id, data: new { prompt.Title }, cancellationToken: cancellationToken);
        return prompt.ToDto(userId, unitOfWork);
    }

    public async Task<PromptDto> UpdateAsync(Guid userId, Guid promptId, PromptUpsertRequest request, CancellationToken cancellationToken = default)
    {
        var prompt = await unitOfWork.Repository<Prompt>().FindAsync(promptId, cancellationToken) ?? throw new InvalidOperationException("Prompt not found.");
        await permissionService.EnsurePromptAsync(userId, prompt.Id, ApplicationPermission.PromptEdit, cancellationToken);

        await EnsureCanWriteTargetAsync(userId, request.ProjectId, request.FolderId, prompt, cancellationToken);

        var createsNewVersion = prompt.Content != request.Content || !string.IsNullOrWhiteSpace(request.Changelog) || !string.IsNullOrWhiteSpace(request.VersionLabel);
        prompt.ProjectId = request.ProjectId;
        prompt.FolderId = request.FolderId;
        prompt.Title = request.Title.Trim();
        prompt.Summary = request.Summary;
        prompt.Content = request.Content;
        prompt.Notes = request.Notes;
        prompt.Status = request.Status;
        prompt.UpdatedAt = DateTimeOffset.UtcNow;

        if (createsNewVersion)
        {
            prompt.CurrentVersionNumber++;
            await unitOfWork.Repository<PromptVersion>().AddAsync(new PromptVersion
            {
                PromptId = prompt.Id,
                AuthorUserId = userId,
                VersionNumber = prompt.CurrentVersionNumber,
                Content = request.Content,
                Label = request.VersionLabel,
                Changelog = request.Changelog,
                Notes = request.VersionNotes
            }, cancellationToken);
        }

        await SyncTagsAsync(userId, prompt.Id, request.Tags, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await auditService.RecordAsync(userId, "prompt.update", "Prompt", prompt.Id, data: new { prompt.Title, createsNewVersion }, cancellationToken: cancellationToken);
        return prompt.ToDto(userId, unitOfWork);
    }

    public async Task DeleteAsync(Guid userId, Guid promptId, CancellationToken cancellationToken = default)
    {
        var prompt = await unitOfWork.Repository<Prompt>().FindAsync(promptId, cancellationToken) ?? throw new InvalidOperationException("Prompt not found.");
        await permissionService.EnsurePromptAsync(userId, prompt.Id, ApplicationPermission.PromptDelete, cancellationToken);

        prompt.IsDeleted = true;
        prompt.DeletedAt = DateTimeOffset.UtcNow;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await auditService.RecordAsync(userId, "prompt.delete", "Prompt", prompt.Id, cancellationToken: cancellationToken);
    }

    public async Task<PromptDto> ToggleFavoriteAsync(Guid userId, Guid promptId, CancellationToken cancellationToken = default)
    {
        var prompt = await unitOfWork.Repository<Prompt>().FindAsync(promptId, cancellationToken) ?? throw new InvalidOperationException("Prompt not found.");
        await permissionService.EnsurePromptAsync(userId, prompt.Id, ApplicationPermission.PromptView, cancellationToken);

        var favorite = await unitOfWork.Repository<Favorite>().Query().FirstOrDefaultAsync(x => x.UserId == userId && x.PromptId == promptId, cancellationToken);
        if (favorite is null)
        {
            await unitOfWork.Repository<Favorite>().AddAsync(new Favorite { UserId = userId, PromptId = promptId }, cancellationToken);
        }
        else
        {
            unitOfWork.Repository<Favorite>().Remove(favorite);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return prompt.ToDto(userId, unitOfWork);
    }

    public async Task<PromptDto> TogglePinnedAsync(Guid userId, Guid promptId, CancellationToken cancellationToken = default)
    {
        var prompt = await unitOfWork.Repository<Prompt>().FindAsync(promptId, cancellationToken) ?? throw new InvalidOperationException("Prompt not found.");
        await permissionService.EnsurePromptAsync(userId, prompt.Id, ApplicationPermission.PromptEdit, cancellationToken);

        prompt.IsPinned = !prompt.IsPinned;
        prompt.UpdatedAt = DateTimeOffset.UtcNow;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return prompt.ToDto(userId, unitOfWork);
    }

    public async Task LogCopyAsync(Guid userId, Guid promptId, bool resolved, CancellationToken cancellationToken = default)
    {
        var prompt = await unitOfWork.Repository<Prompt>().FindAsync(promptId, cancellationToken) ?? throw new InvalidOperationException("Prompt not found.");
        await permissionService.EnsurePromptAsync(userId, prompt.Id, ApplicationPermission.PromptView, cancellationToken);

        prompt.CopyCount++;
        prompt.LastUsedAt = DateTimeOffset.UtcNow;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await auditService.RecordAsync(userId, "prompt.copy", "Prompt", promptId, data: new { resolved }, cancellationToken: cancellationToken);
    }

    private async Task<IQueryable<Prompt>> FilterPromptsAsync(Guid userId, PromptSearchRequest request, CancellationToken cancellationToken)
    {
        var query = unitOfWork.Repository<Prompt>().Query().Where(x => !x.IsDeleted);
        var accessibleProjectIds = AccessControlQueries.AccessibleProjectIds(unitOfWork, userId, SharePermission.View);
        var accessibleFolderIds = await AccessControlQueries.AccessibleFolderIdsAsync(unitOfWork, userId, SharePermission.View, cancellationToken);
        var accessiblePromptIds = AccessControlQueries.AccessiblePromptIds(unitOfWork, userId, SharePermission.View);

        query = query.Where(x =>
            accessibleProjectIds.Contains(x.ProjectId) ||
            (x.FolderId != null && accessibleFolderIds.Contains(x.FolderId.Value)) ||
            accessiblePromptIds.Contains(x.Id));

        if (request.ProjectId is { } projectId) query = query.Where(x => x.ProjectId == projectId);
        if (request.FolderId is { } folderId) query = query.Where(x => x.FolderId == folderId);
        if (request.AuthorUserId is { } authorUserId) query = query.Where(x => x.AuthorUserId == authorUserId);
        if (request.Status is { } status) query = query.Where(x => x.Status == status);

        if (!string.IsNullOrWhiteSpace(request.Query))
        {
            var term = request.Query.Trim();
            query = query.Where(x => x.Title.Contains(term) || x.Content.Contains(term) || (x.Notes != null && x.Notes.Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(request.Tag))
        {
            var tag = request.Tag.Trim();
            var promptIds =
                from promptTag in unitOfWork.Repository<PromptTag>().Query()
                join t in unitOfWork.Repository<Tag>().Query() on promptTag.TagId equals t.Id
                where t.Name == tag
                select promptTag.PromptId;
            query = query.Where(x => promptIds.Contains(x.Id));
        }

        if (request.FavoritesOnly)
        {
            var favoritePromptIds = unitOfWork.Repository<Favorite>().Query().Where(x => x.UserId == userId).Select(x => x.PromptId);
            query = query.Where(x => favoritePromptIds.Contains(x.Id));
        }

        query = request.Sort.Equals("alpha", StringComparison.OrdinalIgnoreCase)
            ? query.OrderBy(x => x.Title)
            : query.OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt);

        if (request.RecentOnly)
        {
            return query.Where(x => x.LastUsedAt != null).OrderByDescending(x => x.LastUsedAt).Take(20);
        }

        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        return query.Skip((page - 1) * pageSize).Take(pageSize);
    }

    private async Task<IReadOnlyList<PromptDto>> MapPromptListAsync(Guid userId, IReadOnlyList<Prompt> prompts, CancellationToken cancellationToken)
    {
        var promptIds = prompts.Select(x => x.Id).ToList();
        var promptTags = await (
                from promptTag in unitOfWork.Repository<PromptTag>().Query()
                join tag in unitOfWork.Repository<Tag>().Query() on promptTag.TagId equals tag.Id
                where promptIds.Contains(promptTag.PromptId)
                orderby tag.Name
                select new { promptTag.PromptId, tag.Name }).ToListAsync(cancellationToken);
        var tagsByPromptId = promptTags
            .GroupBy(x => x.PromptId)
            .ToDictionary(x => x.Key, x => (IReadOnlyList<string>)x.Select(t => t.Name).Distinct().ToList());

        var favoriteIds = await unitOfWork.Repository<Favorite>().Query()
            .Where(x => x.UserId == userId && promptIds.Contains(x.PromptId))
            .Select(x => x.PromptId)
            .ToHashSetAsync(cancellationToken);

        return prompts.Select(prompt => new PromptDto(
            prompt.Id,
            prompt.ProjectId,
            prompt.FolderId,
            prompt.Title,
            prompt.Summary,
            prompt.Content,
            prompt.Notes,
            prompt.Status,
            prompt.CurrentVersionNumber,
            favoriteIds.Contains(prompt.Id),
            prompt.IsPinned,
            prompt.CopyCount,
            prompt.LastUsedAt,
            tagsByPromptId.GetValueOrDefault(prompt.Id, []),
            prompt.CreatedAt,
            prompt.UpdatedAt)).ToList();
    }

    private async Task EnsureCanWriteTargetAsync(Guid userId, Guid projectId, Guid? folderId, Prompt? existingPrompt = null, CancellationToken cancellationToken = default)
    {
        if (existingPrompt is not null && existingPrompt.ProjectId == projectId && existingPrompt.FolderId == folderId)
        {
            return;
        }

        if (folderId is null)
        {
            await permissionService.EnsureProjectAsync(userId, projectId, ApplicationPermission.PromptCreate, cancellationToken);

            return;
        }

        var folder = await unitOfWork.Repository<Folder>().FindAsync(folderId.Value, cancellationToken);
        if (folder is null || folder.IsDeleted || folder.ProjectId != projectId)
        {
            throw new InvalidOperationException("Target folder is not valid for the selected project.");
        }

        await permissionService.EnsureFolderAsync(userId, folder.Id, ApplicationPermission.PromptCreate, cancellationToken);
    }

    private async Task SyncTagsAsync(Guid userId, Guid promptId, IReadOnlyList<string>? tagNames, CancellationToken cancellationToken)
    {
        var existing = await unitOfWork.Repository<PromptTag>().Query().Where(x => x.PromptId == promptId).ToListAsync(cancellationToken);
        foreach (var promptTag in existing)
        {
            unitOfWork.Repository<PromptTag>().Remove(promptTag);
        }

        var normalizedNames = tagNames?
            .Select(x => x.Trim().ToLowerInvariant())
            .Where(x => x.Length > 0)
            .Distinct()
            .ToList() ?? [];
        if (normalizedNames.Count == 0)
        {
            return;
        }

        var existingTags = await unitOfWork.Repository<Tag>().Query()
            .Where(x => x.UserId == userId && normalizedNames.Contains(x.Name))
            .ToDictionaryAsync(x => x.Name, cancellationToken);

        foreach (var name in normalizedNames)
        {
            if (!existingTags.TryGetValue(name, out var tag))
            {
                tag = new Tag { UserId = userId, Name = name, Scope = TagScope.Personal };
                await unitOfWork.Repository<Tag>().AddAsync(tag, cancellationToken);
                existingTags[name] = tag;
            }

            await unitOfWork.Repository<PromptTag>().AddAsync(new PromptTag { PromptId = promptId, TagId = tag.Id }, cancellationToken);
        }
    }
}
