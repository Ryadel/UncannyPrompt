using UncannyPrompt.Domain;

namespace UncannyPrompt.Application;

public interface ICurrentUserContext
{
    Guid? UserId { get; }
    string? Email { get; }
    string? DisplayName { get; }
    bool IsAuthenticated { get; }
}
