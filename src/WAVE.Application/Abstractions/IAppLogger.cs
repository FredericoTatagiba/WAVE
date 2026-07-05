namespace WAVE.Application.Abstractions;

/// <summary>
/// Abstração mínima de log, para a Application não depender de framework de log.
/// Mensagens ao operador nunca expõem detalhes internos; estes vão para o log.
/// </summary>
public interface IAppLogger
{
    void Info(string message);

    void Warn(string message);

    void Error(string message, Exception? exception = null);
}
