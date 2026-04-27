using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using UncannyPrompt.Application;

namespace UncannyPrompt.Infrastructure;

internal sealed class AesSecretProtector(IConfiguration configuration) : ISecretProtector
{
    private readonly byte[] key = SHA256.HashData(Encoding.UTF8.GetBytes(configuration["Security:SecretEncryptionKey"] ?? "dev-secret-encryption-key-change-me"));

    public string Protect(string value)
    {
        var nonce = RandomNumberGenerator.GetBytes(12);
        var plain = Encoding.UTF8.GetBytes(value);
        var cipher = new byte[plain.Length];
        var tag = new byte[16];
        using var aes = new AesGcm(key, tag.Length);
        aes.Encrypt(nonce, plain, cipher, tag);
        return Convert.ToBase64String(nonce.Concat(tag).Concat(cipher).ToArray());
    }

    public string Unprotect(string protectedValue)
    {
        var payload = Convert.FromBase64String(protectedValue);
        var nonce = payload[..12];
        var tag = payload[12..28];
        var cipher = payload[28..];
        var plain = new byte[cipher.Length];
        using var aes = new AesGcm(key, tag.Length);
        aes.Decrypt(nonce, cipher, tag, plain);
        return Encoding.UTF8.GetString(plain);
    }
}
