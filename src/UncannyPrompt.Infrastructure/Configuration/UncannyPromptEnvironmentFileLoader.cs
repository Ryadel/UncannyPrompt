namespace UncannyPrompt.Infrastructure.Configuration;

public static class UncannyPromptEnvironmentFileLoader
{
    public static void Load(string contentRootPath)
    {
        var rootPath = ResolveRootPath(contentRootPath);
        var existingEnvironment = CaptureCurrentEnvironment();

        TryLoad(Path.Combine(rootPath, ".env"));

        var isRunningInContainer = string.Equals(
            Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"),
            "true",
            StringComparison.OrdinalIgnoreCase);

        if (!isRunningInContainer)
        {
            TryLoad(Path.Combine(rootPath, ".env.local"));
        }

        RestoreExistingEnvironment(existingEnvironment);
    }

    private static Dictionary<string, string> CaptureCurrentEnvironment()
    {
        return Environment.GetEnvironmentVariables()
            .Cast<System.Collections.DictionaryEntry>()
            .Where(entry => entry.Key is string && entry.Value is string)
            .ToDictionary(
                entry => (string)entry.Key,
                entry => (string)entry.Value!,
                StringComparer.OrdinalIgnoreCase);
    }

    private static void RestoreExistingEnvironment(Dictionary<string, string> existingEnvironment)
    {
        foreach (var (key, value) in existingEnvironment)
        {
            Environment.SetEnvironmentVariable(key, value);
        }
    }

    private static string ResolveRootPath(string contentRootPath)
    {
        var directory = new DirectoryInfo(contentRootPath);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "UncannyPrompt.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return contentRootPath;
    }

    private static void TryLoad(string path)
    {
        if (File.Exists(path))
        {
            DotNetEnv.Env.Load(path);
        }
    }
}
