using System.Security.Cryptography;

namespace AttendanceApp.Services;

public static class JoinCodeGenerator
{
    // Excludes visually ambiguous characters (0/O, 1/I/L) since students may be
    // typing this in manually off a projector screen.
    private const string Alphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";

    public static string Generate(int length = 6)
    {
        Span<char> code = stackalloc char[length];
        for (int i = 0; i < length; i++)
            code[i] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];
        return new string(code);
    }
}
