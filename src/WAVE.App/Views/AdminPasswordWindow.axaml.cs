using Avalonia.Controls;
using Avalonia.Interactivity;
using WAVE.Application.Abstractions;

namespace WAVE.App.Views;

/// <summary>
/// Asks for the administrator password, or creates it when the device has none yet.
/// </summary>
/// <remarks>
/// There is no sign-in screen, so this is the only place a password is ever typed — and
/// only at the moment an administrator action is attempted.
/// </remarks>
public partial class AdminPasswordWindow : Window
{
    private readonly IAdminSession _session;
    private readonly bool _isFirstUse;

    public AdminPasswordWindow(IAdminSession session)
    {
        InitializeComponent();
        _session = session;
        _isFirstUse = !session.IsConfigured;

        TitleText.Text = _isFirstUse ? "Definir senha de administrador" : "Ação de administrador";
        SubtitleText.Text = _isFirstUse
            ? "Esta é a primeira ação administrativa neste dispositivo. Defina a senha que passará a proteger o cadastro de redes e as configurações."
            : "Informe a senha de administrador para continuar.";
        SubmitButton.Content = _isFirstUse ? "Definir e continuar" : "Desbloquear";
        ConfirmPanel.IsVisible = _isFirstUse;

        Loaded += (_, _) => PasswordInput.Focus();
    }

    /// <summary>Whether administrator actions ended up unlocked.</summary>
    public bool Unlocked { get; private set; }

    private async void OnSubmit(object? sender, RoutedEventArgs e)
    {
        var password = PasswordInput.Text ?? string.Empty;

        if (_isFirstUse && !string.Equals(password, ConfirmInput.Text ?? string.Empty, StringComparison.Ordinal))
        {
            ShowError("As senhas não coincidem.");
            return;
        }

        var result = _isFirstUse
            ? await _session.ConfigureAsync(password)
            : await _session.UnlockAsync(password);

        if (result.IsFailure)
        {
            ShowError(result.Error);
            return;
        }

        Unlocked = true;
        Close();
    }

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorText.IsVisible = true;
    }
}
