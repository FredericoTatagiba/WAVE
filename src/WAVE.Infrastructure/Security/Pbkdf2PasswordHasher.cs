using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using WAVE.Application.Abstractions;

namespace WAVE.Infrastructure.Security;

/// <summary>
/// Password hashing with PBKDF2 (SHA-256) and a random salt. Hash format:
/// <c>iterations:saltBase64:hashBase64</c>. The password is never persisted.
/// </summary>
public sealed class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const int Iterations = 120_000;
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const char Separator = ':';

    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password ?? string.Empty), salt, Iterations, HashAlgorithmName.SHA256, HashSize);

        return string.Join(
            Separator,
            Iterations.ToString(CultureInfo.InvariantCulture),
            Convert.ToBase64String(salt),
            Convert.ToBase64String(hash));
    }

    public bool Verify(string password, string hash)
    {
        if (string.IsNullOrEmpty(hash))
        {
            return false;
        }

        try
        {
            var parts = hash.Split(Separator);
            if (parts.Length != 3)
            {
                return false;
            }

            var iterations = int.Parse(parts[0], CultureInfo.InvariantCulture);
            var salt = Convert.FromBase64String(parts[1]);
            var expected = Convert.FromBase64String(parts[2]);

            var actual = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(password ?? string.Empty), salt, iterations, HashAlgorithmName.SHA256, expected.Length);

            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch
        {
            return false;
        }
    }
}
