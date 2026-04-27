using UncannyPrompt.Domain;

namespace UncannyPrompt.Application;

public sealed record VersionCreateRequest(string Content, string? Label, string? Changelog, string? Notes);
