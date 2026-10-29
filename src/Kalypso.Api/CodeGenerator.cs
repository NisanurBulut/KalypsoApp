using System.Security.Cryptography;

namespace Kalypso.Api;

/// <summary>Makes random short codes. Letters that look alike (0/O, 1/l/I) are left out so codes are easy to read aloud.</summary>
public static class CodeGenerator
{
    public const int Length = 7;
    public const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789";

    public static string NewCode() => RandomNumberGenerator.GetString(Alphabet, Length);
}
