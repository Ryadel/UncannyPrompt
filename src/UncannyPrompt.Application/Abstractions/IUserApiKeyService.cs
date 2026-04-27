namespace UncannyPrompt.Application;

public interface IUserApiKeyService
{
    Task<IReadOnlyList<ApiKeyDto>> ListAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<ApiKeyCreateResultDto> CreateAsync(Guid userId, ApiKeyCreateRequest request, CancellationToken cancellationToken = default);
    Task<bool> RevokeAsync(Guid userId, Guid apiKeyId, CancellationToken cancellationToken = default);
}
