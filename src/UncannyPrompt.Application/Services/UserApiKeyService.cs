using Microsoft.EntityFrameworkCore;
using UncannyPrompt.Domain;

namespace UncannyPrompt.Application;

internal sealed class UserApiKeyService(IUnitOfWork unitOfWork, IApiKeyHasher apiKeyHasher) : IUserApiKeyService
{
    private const string KeyPrefix = "upk_";
    private const int DisplayPrefixLength = 16;

    public async Task<IReadOnlyList<ApiKeyDto>> ListAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await unitOfWork.Repository<UserApiKey>().Query()
            .Where(x => x.UserId == userId && !x.IsDeleted)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new ApiKeyDto(x.Id, x.Name, x.Prefix, x.CreatedAt, x.ExpiresAt, x.LastUsedAt))
            .ToListAsync(cancellationToken);

    public async Task<ApiKeyCreateResultDto> CreateAsync(Guid userId, ApiKeyCreateRequest request, CancellationToken cancellationToken = default)
    {
        var name = (request.Name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("API key name is required.", nameof(request));
        }

        if (name.Length > 120)
        {
            throw new ArgumentException("API key name must be at most 120 characters.", nameof(request));
        }

        if (request.ExpiresAt is { } expiresAt && expiresAt <= DateTimeOffset.UtcNow)
        {
            throw new ArgumentException("API key expiration must be in the future.", nameof(request));
        }

        var plainTextKey = KeyPrefix + apiKeyHasher.CreateToken();
        var displayPrefix = plainTextKey[..Math.Min(DisplayPrefixLength, plainTextKey.Length)];
        var apiKey = new UserApiKey
        {
            UserId = userId,
            Name = name,
            Prefix = displayPrefix,
            KeyHash = apiKeyHasher.Hash(plainTextKey),
            ExpiresAt = request.ExpiresAt
        };

        await unitOfWork.Repository<UserApiKey>().AddAsync(apiKey, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new ApiKeyCreateResultDto(ToDto(apiKey), plainTextKey);
    }

    public async Task<bool> RevokeAsync(Guid userId, Guid apiKeyId, CancellationToken cancellationToken = default)
    {
        var apiKey = await unitOfWork.Repository<UserApiKey>().FindAsync(apiKeyId, cancellationToken);
        if (apiKey is null || apiKey.UserId != userId || apiKey.IsDeleted)
        {
            return false;
        }

        apiKey.IsDeleted = true;
        apiKey.DeletedAt = DateTimeOffset.UtcNow;
        apiKey.UpdatedAt = apiKey.DeletedAt;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static ApiKeyDto ToDto(UserApiKey apiKey) =>
        new(apiKey.Id, apiKey.Name, apiKey.Prefix, apiKey.CreatedAt, apiKey.ExpiresAt, apiKey.LastUsedAt);
}
