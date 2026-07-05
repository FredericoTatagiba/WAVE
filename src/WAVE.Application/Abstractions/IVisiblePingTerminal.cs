namespace WAVE.Application.Abstractions;

/// <summary>
/// Abre a janela de terminal com ping contínuo visível ao técnico
/// (persistência do terminal exigida pela especificação).
/// </summary>
public interface IVisiblePingTerminal
{
    void Launch(string host);

    void Close();
}
