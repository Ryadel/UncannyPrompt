using Microsoft.Extensions.Configuration;

namespace UncannyPrompt.UnitTests.TestSupport;

internal static class TestConfiguration
{
    public static IConfiguration Create() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Security:ApiKeyHashKey"] = "unit-test-api-key-hash-key-with-enough-entropy",
                ["Security:SecretEncryptionKey"] = "unit-test-secret-encryption-key-with-enough-entropy"
            })
            .Build();
}
