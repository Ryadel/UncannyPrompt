using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using UncannyPrompt.Domain;

namespace UncannyPrompt.Application;

internal sealed class PromptVersionService(IUnitOfWork unitOfWork, IPermissionService permissionService, IAuditService auditService) : IPromptVersionService
{
    public async Task<IReadOnlyList<PromptVersionDto>> ListAsync(Guid userId, Guid promptId, CancellationToken cancellationToken = default)
    {
        var prompt = await unitOfWork.Repository<Prompt>().FindAsync(promptId, cancellationToken) ?? throw new InvalidOperationException("Prompt not found.");
        await permissionService.EnsurePromptAsync(userId, prompt.Id, ApplicationPermission.VersionView, cancellationToken);

        return await unitOfWork.Repository<PromptVersion>().Query()
            .Where(x => x.PromptId == promptId)
            .OrderByDescending(x => x.VersionNumber)
            .Take(200)
            .Select(x => x.ToDto())
            .ToListAsync(cancellationToken);
    }

    public async Task<PromptVersionDto> CreateAsync(Guid userId, Guid promptId, VersionCreateRequest request, CancellationToken cancellationToken = default)
    {
        var prompt = await unitOfWork.Repository<Prompt>().FindAsync(promptId, cancellationToken) ?? throw new InvalidOperationException("Prompt not found.");
        await permissionService.EnsurePromptAsync(userId, prompt.Id, ApplicationPermission.PromptEdit, cancellationToken);

        prompt.CurrentVersionNumber++;
        prompt.Content = request.Content;
        prompt.UpdatedAt = DateTimeOffset.UtcNow;
        var version = new PromptVersion
        {
            PromptId = prompt.Id,
            AuthorUserId = userId,
            VersionNumber = prompt.CurrentVersionNumber,
            Content = request.Content,
            Label = request.Label,
            Changelog = request.Changelog,
            Notes = request.Notes
        };
        await unitOfWork.Repository<PromptVersion>().AddAsync(version, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await auditService.RecordAsync(userId, "version.create", "PromptVersion", version.Id, data: new { promptId }, cancellationToken: cancellationToken);
        return version.ToDto();
    }

    public async Task<PromptDto> RestoreAsync(Guid userId, Guid promptId, Guid versionId, string? changelog, CancellationToken cancellationToken = default)
    {
        var prompt = await unitOfWork.Repository<Prompt>().FindAsync(promptId, cancellationToken) ?? throw new InvalidOperationException("Prompt not found.");
        var source = await unitOfWork.Repository<PromptVersion>().FindAsync(versionId, cancellationToken) ?? throw new InvalidOperationException("Version not found.");
        if (source.PromptId != prompt.Id)
        {
            throw new UnauthorizedAccessException("Version restore denied.");
        }
        await permissionService.EnsurePromptAsync(userId, prompt.Id, ApplicationPermission.VersionRestore, cancellationToken);

        prompt.CurrentVersionNumber++;
        prompt.Content = source.Content;
        prompt.UpdatedAt = DateTimeOffset.UtcNow;
        await unitOfWork.Repository<PromptVersion>().AddAsync(new PromptVersion
        {
            PromptId = prompt.Id,
            AuthorUserId = userId,
            VersionNumber = prompt.CurrentVersionNumber,
            Content = source.Content,
            Label = $"restore-{source.VersionNumber}",
            Changelog = string.IsNullOrWhiteSpace(changelog) ? $"Restored from version {source.VersionNumber}." : changelog
        }, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await auditService.RecordAsync(userId, "version.restore", "Prompt", prompt.Id, data: new { versionId }, cancellationToken: cancellationToken);
        return prompt.ToDto(userId, unitOfWork);
    }

    public async Task<string> DiffAsync(Guid userId, Guid leftVersionId, Guid rightVersionId, CancellationToken cancellationToken = default)
    {
        var left = await unitOfWork.Repository<PromptVersion>().FindAsync(leftVersionId, cancellationToken) ?? throw new InvalidOperationException("Left version not found.");
        var right = await unitOfWork.Repository<PromptVersion>().FindAsync(rightVersionId, cancellationToken) ?? throw new InvalidOperationException("Right version not found.");
        var prompt = await unitOfWork.Repository<Prompt>().FindAsync(left.PromptId, cancellationToken) ?? throw new InvalidOperationException("Prompt not found.");
        if (left.PromptId != right.PromptId)
        {
            throw new UnauthorizedAccessException("Diff access denied.");
        }
        await permissionService.EnsurePromptAsync(userId, prompt.Id, ApplicationPermission.VersionView, cancellationToken);

        var oldLines = left.Content.Replace("\r\n", "\n").Split('\n');
        var newLines = right.Content.Replace("\r\n", "\n").Split('\n');
        return string.Join(Environment.NewLine, oldLines.Except(newLines).Select(x => $"- {x}").Concat(newLines.Except(oldLines).Select(x => $"+ {x}")));
    }
}
