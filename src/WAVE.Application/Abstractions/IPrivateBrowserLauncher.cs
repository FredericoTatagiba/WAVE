namespace WAVE.Application.Abstractions;

/// <summary>
/// Abre uma URL em janela anônima/privativa de instância limpa do navegador,
/// evitando interferência de cache/sessões anteriores.
/// </summary>
public interface IPrivateBrowserLauncher
{
    void Launch(string url);
}
