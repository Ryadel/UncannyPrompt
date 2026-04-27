using UncannyPrompt.Domain;

namespace UncannyPrompt.Application;

public interface IVariableService
{
    Task<IReadOnlyList<VariableDto>> ListAsync(Guid userId, Guid? projectId = null, CancellationToken cancellationToken = default);
    Task<VariableDto> UpsertAsync(Guid userId, VariableUpsertRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid userId, Guid variableDefinitionId, CancellationToken cancellationToken = default);
}
