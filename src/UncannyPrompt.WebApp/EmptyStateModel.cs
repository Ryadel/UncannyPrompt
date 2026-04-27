namespace UncannyPrompt.WebApp;

public sealed record EmptyStateModel(
    string Title,
    string Description,
    string PrimaryActionLabel,
    string PrimaryActionPage,
    string? PrimaryActionFragment = null,
    EmptyStateIcon Icon = EmptyStateIcon.Workspace,
    EmptyStateTone Tone = EmptyStateTone.Default);

public enum EmptyStateIcon
{
    Workspace,
    Project,
    Lock,
}

public enum EmptyStateTone
{
    Default,
    Blocker,
}
