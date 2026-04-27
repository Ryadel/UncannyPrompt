namespace UncannyPrompt.WebApp;

public sealed record AppPageHeaderModel(
    string Title,
    IReadOnlyList<string>? KickerParts = null,
    AppPageHeaderAction? Action = null);

public sealed record AppPageHeaderAction(
    string Label,
    string Page,
    IDictionary<string, string>? RouteValues = null);
