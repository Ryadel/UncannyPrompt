using UncannyPrompt.Domain;

namespace UncannyPrompt.Application;

public interface IApiKeyHasher
{
    string Hash(string apiKey);
    bool Verify(string apiKey, string hash);
    string CreateToken(int bytes = 32);
}
