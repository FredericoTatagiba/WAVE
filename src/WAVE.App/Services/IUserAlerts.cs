namespace WAVE.App.Services;

/// <summary>Abstração de alertas ao usuário, para desacoplar as ViewModels da UI.</summary>
public interface IUserAlerts
{
    void Error(string message);

    void Info(string message);
}
