namespace WAVE.Application.Abstractions;

/// <summary>Gera e verifica hashes de senha (algoritmo forte, com sal).</summary>
public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string password, string hash);
}
