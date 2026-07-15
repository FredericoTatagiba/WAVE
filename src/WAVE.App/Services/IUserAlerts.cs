namespace WAVE.App.Services;

/// <summary>User-alert abstraction, to decouple the ViewModels from the UI.</summary>
public interface IUserAlerts
{
    void Error(string message);

    void Info(string message);
}
