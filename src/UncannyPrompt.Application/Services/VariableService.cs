using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using UncannyPrompt.Domain;

namespace UncannyPrompt.Application;

internal sealed class VariableService(IUnitOfWork unitOfWork, IPermissionService permissionService, ISecretProtector secretProtector) : IVariableService
{
    public async Task<IReadOnlyList<VariableDto>> ListAsync(Guid userId, Guid? projectId = null, CancellationToken cancellationToken = default)
    {
        if (projectId is { } requestedProjectId &&
            !await permissionService.CanAccessProjectAsync(userId, requestedProjectId, ApplicationPermission.PromptView, cancellationToken))
        {
            throw new UnauthorizedAccessException("Variable access denied.");
        }

        var definitions = await unitOfWork.Repository<VariableDefinition>().Query()
            .Where(x => !x.IsDeleted && (x.UserId == userId || (projectId != null && x.ProjectId == projectId)))
            .OrderBy(x => x.Name)
            .Take(500)
            .ToListAsync(cancellationToken);
        var values = await unitOfWork.Repository<VariableValue>().Query()
            .Where(x => !x.IsDeleted && (x.UserId == userId || (projectId != null && x.ProjectId == projectId)))
            .ToListAsync(cancellationToken);

        var result = definitions.Select(definition =>
        {
            var value = values.FirstOrDefault(x => x.VariableDefinitionId == definition.Id);
            var visibleValue = definition.IsSecret && value?.Value is not null ? "********" : value?.Value;
            return new VariableDto(definition.Id, definition.Name, definition.Description, definition.DefaultValue, definition.IsRequired, definition.IsSecret, definition.Scope, definition.ProjectId, visibleValue);
        }).ToList();

        return result;
    }

    public async Task<VariableDto> UpsertAsync(Guid userId, VariableUpsertRequest request, CancellationToken cancellationToken = default)
    {
        if (!PromptResolutionService.VariableNameRegex().IsMatch(request.Name))
        {
            throw new InvalidOperationException("Variable names must use letters, numbers and underscores.");
        }

        var definition = request.Id is { } id
            ? await unitOfWork.Repository<VariableDefinition>().FindAsync(id, cancellationToken) ?? throw new InvalidOperationException("Variable not found.")
            : new VariableDefinition { UserId = request.Scope == VariableScope.User ? userId : null };

        await EnsureCanManageDefinitionAsync(userId, definition, request.ProjectId, request.Scope, cancellationToken);

        if (request.Id is null)
        {
            await unitOfWork.Repository<VariableDefinition>().AddAsync(definition, cancellationToken);
        }

        definition.Name = request.Name.Trim();
        definition.Description = request.Description;
        definition.DefaultValue = request.DefaultValue;
        definition.IsRequired = request.IsRequired;
        definition.IsSecret = request.IsSecret;
        definition.Scope = request.Scope;
        definition.ProjectId = request.ProjectId;
        definition.UpdatedAt = DateTimeOffset.UtcNow;

        if (request.Value is not null)
        {
            var value = await unitOfWork.Repository<VariableValue>().Query()
                .FirstOrDefaultAsync(x => x.VariableDefinitionId == definition.Id && x.UserId == userId && x.ProjectId == request.ProjectId, cancellationToken)
                ?? new VariableValue { VariableDefinitionId = definition.Id, UserId = userId, ProjectId = request.ProjectId };
            value.Value = request.IsSecret ? secretProtector.Protect(request.Value) : request.Value;
            value.IsEncrypted = request.IsSecret;
            if (!await unitOfWork.Repository<VariableValue>().Query().AnyAsync(x => x.Id == value.Id, cancellationToken))
            {
                await unitOfWork.Repository<VariableValue>().AddAsync(value, cancellationToken);
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new VariableDto(definition.Id, definition.Name, definition.Description, definition.DefaultValue, definition.IsRequired, definition.IsSecret, definition.Scope, definition.ProjectId, request.IsSecret ? "********" : request.Value);
    }

    public async Task DeleteAsync(Guid userId, Guid variableDefinitionId, CancellationToken cancellationToken = default)
    {
        var definition = await unitOfWork.Repository<VariableDefinition>().FindAsync(variableDefinitionId, cancellationToken) ?? throw new InvalidOperationException("Variable not found.");
        if (definition.Scope == VariableScope.User && definition.UserId != userId)
        {
            throw new UnauthorizedAccessException("Variable delete denied.");
        }

        if (definition.ProjectId is { } projectId)
        {
            await permissionService.EnsureProjectAsync(userId, projectId, ApplicationPermission.VariableManage, cancellationToken);
        }

        definition.IsDeleted = true;
        definition.DeletedAt = DateTimeOffset.UtcNow;
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureCanManageDefinitionAsync(Guid userId, VariableDefinition definition, Guid? requestProjectId, VariableScope requestScope, CancellationToken cancellationToken)
    {
        if (definition.Id != default && definition.Scope == VariableScope.User && definition.UserId != userId)
        {
            throw new UnauthorizedAccessException("Variable edit denied.");
        }

        var projectId = requestProjectId ?? definition.ProjectId;
        if (requestScope == VariableScope.Project && projectId is null)
        {
            throw new InvalidOperationException("Project variables require a project id.");
        }

        if (projectId is { } targetProjectId)
        {
            await permissionService.EnsureProjectAsync(userId, targetProjectId, ApplicationPermission.VariableManage, cancellationToken);
        }
    }
}
