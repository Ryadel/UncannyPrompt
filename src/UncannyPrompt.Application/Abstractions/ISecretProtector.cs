using UncannyPrompt.Domain;

namespace UncannyPrompt.Application;

public interface ISecretProtector
{
    string Protect(string value);
    string Unprotect(string protectedValue);
}
