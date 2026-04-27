using UncannyPrompt.Domain;

namespace UncannyPrompt.Application;

public interface IUserService
{
    Task<UserDto> ProvisionExternalUserAsync(string provider, string subjectId, string? email, string? displayName, CancellationToken cancellationToken = default);
    Task<UserDto?> GetAsync(Guid userId, CancellationToken cancellationToken = default);
}
