using Avalonia;

namespace WAVE.App;

/// <summary>
/// Entry point. WPF generated this from the Application markup; Avalonia requires it to
/// be written out, because the AppBuilder configuration lives here.
/// </summary>
internal static class Program
{
    [STAThread]
    public static void Main(string[] args) =>
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

    /// <summary>Also used by the Avalonia XAML previewer, which requires this exact name.</summary>
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
