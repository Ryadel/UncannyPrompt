using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using UncannyPrompt.Domain;

namespace UncannyPrompt.Application;

internal sealed partial class PromptResolutionService(IUnitOfWork unitOfWork, ITenantScopeService scopeService, ISecretProtector secretProtector) : IPromptResolutionService
{
    public IReadOnlyList<string> ExtractPlaceholders(string content) =>
        PlaceholderRegex().Matches(content).Select(x => x.Groups["name"].Value).Distinct().OrderBy(x => x).ToList();

    public async Task<PromptResolutionResult> ResolveAsync(Guid userId, Guid promptId, IDictionary<string, string?> userValues, CancellationToken cancellationToken = default)
    {
        var prompt = await unitOfWork.Repository<Prompt>().FindAsync(promptId, cancellationToken) ?? throw new InvalidOperationException("Prompt not found.");
        if (!await scopeService.CanAccessPromptAsync(userId, prompt.Id, cancellationToken: cancellationToken))
        {
            throw new UnauthorizedAccessException("Prompt access denied.");
        }

        var project = await unitOfWork.Repository<Project>().FindAsync(prompt.ProjectId, cancellationToken) ?? throw new InvalidOperationException("Project not found.");
        var workspace = await unitOfWork.Repository<Workspace>().FindAsync(project.WorkspaceId, cancellationToken) ?? throw new InvalidOperationException("Workspace not found.");
        var placeholders = ExtractPlaceholders(prompt.Content);
        var definitions = await unitOfWork.Repository<VariableDefinition>().Query()
            .Where(x => !x.IsDeleted &&
                        (x.Scope == VariableScope.System ||
                         x.UserId == userId ||
                         x.ProjectId == prompt.ProjectId ||
                         x.WorkspaceId == workspace.Id ||
                         x.TenantId == workspace.TenantId))
            .ToListAsync(cancellationToken);
        var values = await unitOfWork.Repository<VariableValue>().Query()
            .Where(x => !x.IsDeleted &&
                        (x.UserId == userId ||
                         x.ProjectId == prompt.ProjectId ||
                         x.PromptId == prompt.Id))
            .ToListAsync(cancellationToken);
        var missing = new List<string>();
        var resolved = PlaceholderRegex().Replace(prompt.Content, match =>
        {
            var name = match.Groups["name"].Value;
            var value = ResolveValue(name, prompt, workspace, userId, userValues, definitions, values);
            if (string.IsNullOrEmpty(value))
            {
                missing.Add(name);
                return match.Value;
            }

            return value;
        });

        return new PromptResolutionResult(prompt.Content, resolved, placeholders, missing.Distinct().OrderBy(x => x).ToList());
    }

    private string? ResolveValue(string name, Prompt prompt, Workspace workspace, Guid userId, IDictionary<string, string?> userValues, IReadOnlyList<VariableDefinition> definitions, IReadOnlyList<VariableValue> values)
    {
        if (userValues.TryGetValue(name, out var directValue) && !string.IsNullOrWhiteSpace(directValue))
        {
            return directValue;
        }

        foreach (var definition in definitions
            .Where(x => x.Name == name && DefinitionApplies(x, prompt, workspace, userId))
            .OrderByDescending(DefinitionPrecedence))
        {
            var storedValue = values
                .Where(x => x.VariableDefinitionId == definition.Id)
                .OrderByDescending(x => x.PromptId == prompt.Id)
                .ThenByDescending(x => x.ProjectId == prompt.ProjectId)
                .ThenByDescending(x => x.UserId == userId)
                .FirstOrDefault();

            if (storedValue?.Value is { Length: > 0 } value)
            {
                return storedValue.IsEncrypted ? secretProtector.Unprotect(value) : value;
            }

            if (!string.IsNullOrEmpty(definition.DefaultValue))
            {
                return definition.DefaultValue;
            }
        }

        return null;
    }

    private static bool DefinitionApplies(VariableDefinition definition, Prompt prompt, Workspace workspace, Guid userId) =>
        definition.Scope switch
        {
            VariableScope.Prompt => definition.ProjectId == prompt.ProjectId,
            VariableScope.Project => definition.ProjectId == prompt.ProjectId,
            VariableScope.Workspace => definition.WorkspaceId == workspace.Id,
            VariableScope.Tenant => definition.TenantId == workspace.TenantId,
            VariableScope.User => definition.UserId == userId,
            VariableScope.System => true,
            _ => false
        };

    private static int DefinitionPrecedence(VariableDefinition definition) =>
        definition.Scope switch
        {
            VariableScope.Prompt => 5,
            VariableScope.Project => 4,
            VariableScope.Workspace => 3,
            VariableScope.Tenant => 3,
            VariableScope.User => 2,
            VariableScope.System => 1,
            _ => 0
        };

    [GeneratedRegex(@"\{\{\s*(?<name>[a-zA-Z][a-zA-Z0-9_]*)\s*\}\}", RegexOptions.Compiled)]
    private static partial Regex PlaceholderRegex();

    [GeneratedRegex(@"^[a-zA-Z][a-zA-Z0-9_]*$", RegexOptions.Compiled)]
    public static partial Regex VariableNameRegex();
}
