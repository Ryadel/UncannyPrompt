namespace UncannyPrompt.Domain;

public sealed class User : Entity
{
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Standard;
    public UserStatus Status { get; set; } = UserStatus.Active;
    public string? LastLoginProvider { get; set; }
    public DateTimeOffset? LastLoginAt { get; set; }
    public DateTimeOffset? OnboardingDismissedAt { get; set; }
    public ICollection<AuthIdentity> AuthIdentities { get; set; } = [];
    public ICollection<UserApiKey> ApiKeys { get; set; } = [];
    public ICollection<TenantMembership> TenantMemberships { get; set; } = [];
    public ICollection<Favorite> Favorites { get; set; } = [];
}
