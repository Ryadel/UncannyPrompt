namespace UncannyPrompt.Application;

public interface IPublicTokenHasher
{
    string CreateToken(int bytes = 32);
    string CreateLookupHash(string token);
    string HashToken(string token);
    bool VerifyToken(string token, string hash);
}
