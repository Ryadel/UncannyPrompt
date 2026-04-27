using UncannyPrompt.Infrastructure;
using UncannyPrompt.UnitTests.TestSupport;

namespace UncannyPrompt.UnitTests.Security;

public sealed class ApiKeyHasherTests
{
    [Fact]
    public void Verify_ReturnsTrue_ForOriginalApiKey()
    {
        var hasher = new ApiKeyHasher(TestConfiguration.Create());
        var apiKey = "up_live_test_key";

        var hash = hasher.Hash(apiKey);

        Assert.True(hasher.Verify(apiKey, hash));
    }

    [Fact]
    public void Verify_ReturnsFalse_ForDifferentApiKey()
    {
        var hasher = new ApiKeyHasher(TestConfiguration.Create());
        var hash = hasher.Hash("up_live_test_key");

        Assert.False(hasher.Verify("up_live_other_key", hash));
    }

    [Fact]
    public void CreateToken_ReturnsUrlSafeOpaqueToken()
    {
        var hasher = new ApiKeyHasher(TestConfiguration.Create());

        var token = hasher.CreateToken();

        Assert.NotEmpty(token);
        Assert.DoesNotContain("+", token);
        Assert.DoesNotContain("/", token);
        Assert.DoesNotContain("=", token);
    }
}
