namespace WAVE.Application.Abstractions;

/// <summary>
/// Verifica a senha administrativa contra um hash forte (PBKDF2), sem manter
/// a senha em texto claro. Implementado na Infraestrutura.
/// </summary>
public interface IAdminPasswordVerifier
{
    bool Verify(string password);

    /// <summary>Indica se há uma senha administrativa configurada.</summary>
    bool IsConfigured { get; }
}
