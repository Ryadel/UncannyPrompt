using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using UncannyPrompt.Application;

namespace UncannyPrompt.Infrastructure;

internal sealed class PublicTokenHasher(IConfiguration configuration) : IPublicTokenHasher
{
    private const int SaltBytes = 16;
    private const int HashBytes = 32;
    private const int Iterations = 100_000;
    private readonly byte[] lookupKey = Encoding.UTF8.GetBytes(
        configuration["Security:PublicLinkLookupHashKey"] ??
        configuration["Security:ApiKeyHashKey"] ??
        "dev-public-link-lookup-hash-key-change-me");

    public string CreateToken(int bytes = 32) =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(bytes)).Replace("+", "-").Replace("/", "_").TrimEnd('=');

    public string CreateLookupHash(string token)
    {
        using var hmac = new HMACSHA256(lookupKey);
        return Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(token)));
    }

    public string HashToken(string token)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltBytes);
        var hash = Rfc2898DeriveBytes.Pbkdf2(token, salt, Iterations, HashAlgorithmName.SHA256, HashBytes);
        return $"pbkdf2-sha256${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public bool VerifyToken(string token, string hash)
    {
        var parts = hash.Split('$');
        if (parts.Length != 4 || parts[0] != "pbkdf2-sha256" || !int.TryParse(parts[1], out var iterations))
        {
            return FixedTimeEquals(CreateLookupHash(token), hash);
        }

        var salt = Convert.FromBase64String(parts[2]);
        var expected = Convert.FromBase64String(parts[3]);
        var actual = Rfc2898DeriveBytes.Pbkdf2(token, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }

    private static bool FixedTimeEquals(string left, string right)
    {
        var leftBytes = Encoding.UTF8.GetBytes(left);
        var rightBytes = Encoding.UTF8.GetBytes(right);
        return leftBytes.Length == rightBytes.Length && CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }
}
