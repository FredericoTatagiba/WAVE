namespace WAVE.Application.Abstractions;

/// <summary>Generates and verifies password hashes (strong algorithm, salted).</summary>
public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string password, string hash);
}
