using System.Security.Cryptography;

namespace KitobdaGimen.Application.Common;

/// <summary>
/// Generates short, URL-friendly random identifiers (base62) used as the public
/// slug for posts in <c>/post/{username}/{slug}</c>.
/// </summary>
public static class SlugGenerator
{
    private const string Alphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    /// <summary>Returns a cryptographically-random slug of the given length (default 12).</summary>
    public static string Generate(int length = 12)
    {
        var bytes = RandomNumberGenerator.GetBytes(length);
        var chars = new char[length];
        for (var i = 0; i < length; i++)
        {
            chars[i] = Alphabet[bytes[i] % Alphabet.Length];
        }
        return new string(chars);
    }
}
