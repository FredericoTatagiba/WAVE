namespace WAVE.Application.Abstractions;

/// <summary>
/// Minimal logging abstraction, so the Application does not depend on a logging framework.
/// Operator messages never expose internal details; those go to the log.
/// </summary>
public interface IAppLogger
{
    void Info(string message);

    void Warn(string message);

    void Error(string message, Exception? exception = null);
}
