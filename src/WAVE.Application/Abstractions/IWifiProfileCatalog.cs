namespace WAVE.Application.Abstractions;

/// <summary>
/// Consulta os perfis de rede que o Windows já tem salvos. Quando um perfil
/// existe, é possível conectar sem informar a senha novamente.
/// </summary>
public interface IWifiProfileCatalog
{
    Task<IReadOnlyList<string>> GetSavedProfileNamesAsync(CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(string ssid, CancellationToken cancellationToken = default);
}
