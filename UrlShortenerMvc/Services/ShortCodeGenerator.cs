using System.Security.Cryptography;

namespace UrlShortenerMvc.Services;

public interface IShortCodeGenerator
{
    string Generate(int length);
}

internal class ShortCodeGenerator : IShortCodeGenerator
{
    private const string Characters = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

    public string Generate(int length)
    {
        if (length is < 6 or > 8)
            throw new ArgumentOutOfRangeException(
                nameof(length), "Short code length must be between 6 and 8.");

        Span<char> result = stackalloc char[length];

        for (var i = 0; i < length; i++)
            result[i] = Characters[RandomNumberGenerator.GetInt32(Characters.Length)];

        return new string(result);
    }
}