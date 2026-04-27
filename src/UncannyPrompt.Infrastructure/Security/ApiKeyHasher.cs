using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using UncannyPrompt.Application;

namespace UncannyPrompt.Infrastructure;

internal sealed class ApiKeyHasher(IConfiguration configuration) : IApiKeyHasher
{
    private readonly byte[] key = Encoding.UTF8.GetBytes(configuration["Security:ApiKeyHashKey"] ?? "dev-api-key-hash-key-change-me");

    public string Hash(string apiKey)
    {
        using var hmac = new HMACSHA256(key);
        return Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(apiKey)));
    }

    public bool Verify(string apiKey, string hash)
    {
        var left = Convert.FromBase64String(Hash(apiKey));
        var right = Convert.FromBase64String(hash);
        return CryptographicOperations.FixedTimeEquals(left, right);
    }

    public string CreateToken(int bytes = 32) =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(bytes)).Replace("+", "-").Replace("/", "_").TrimEnd('=');
}
