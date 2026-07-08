using WAVE.Domain.Networking;

namespace WAVE.App.Services;

/// <summary>Solicita ao usuário a credencial de uma rede protegida (diálogo modal).</summary>
public interface ICredentialPrompt
{
    /// <summary>Retorna a credencial informada, ou null se o usuário cancelar.</summary>
    WifiSecret? Request(WifiNetworkProfile profile);
}
