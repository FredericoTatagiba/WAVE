using CommunityToolkit.Mvvm.ComponentModel;
using WAVE.Application.Security;

namespace WAVE.App.ViewModels;

/// <summary>Login ViewModel. Toggles between "create administrator" (first run) and "sign in".</summary>
public sealed class LoginViewModel : ObservableObject
{
    private readonly AuthenticationService _authentication;

    private string _username = string.Empty;
    private string _displayName = string.Empty;
    private string _error = string.Empty;
    private bool _isFirstRun;
    private bool _isBusy;

    public LoginViewModel(AuthenticationService authentication) => _authentication = authentication;

    public string Username
    {
        get => _username;
        set => SetProperty(ref _username, value);
    }

    public string DisplayName
    {
        get => _displayName;
        set => SetProperty(ref _displayName, value);
    }

    public string Error
    {
        get => _error;
        private set
        {
            if (SetProperty(ref _error, value))
            {
                OnPropertyChanged(nameof(HasError));
            }
        }
    }

    public bool HasError => !string.IsNullOrEmpty(_error);

    public bool IsBusy
    {
        get => _isBusy;
        private set => SetProperty(ref _isBusy, value);
    }

    public bool IsFirstRun
    {
        get => _isFirstRun;
        private set
        {
            if (SetProperty(ref _isFirstRun, value))
            {
                OnPropertyChanged(nameof(Title));
                OnPropertyChanged(nameof(ActionText));
                OnPropertyChanged(nameof(ShowSetupFields));
            }
        }
    }

    public string Title => IsFirstRun ? "Bem-vindo — crie o administrador" : "Entrar no WAVE";

    public string ActionText => IsFirstRun ? "Criar e entrar" : "Entrar";

    public bool ShowSetupFields => IsFirstRun;

    public async Task InitializeAsync() => IsFirstRun = await _authentication.IsFirstRunAsync();

    public async Task<bool> SubmitAsync(string password, string confirmPassword)
    {
        Error = string.Empty;
        IsBusy = true;
        try
        {
            if (IsFirstRun)
            {
                if (!string.Equals(password, confirmPassword, StringComparison.Ordinal))
                {
                    Error = "As senhas não coincidem.";
                    return false;
                }

                var created = await _authentication.CreateInitialAdministratorAsync(Username, DisplayName, password);
                if (created.IsFailure)
                {
                    Error = created.Error;
                    return false;
                }
            }

            var login = await _authentication.AuthenticateAsync(Username, password);
            if (login.IsFailure)
            {
                Error = login.Error;
                return false;
            }

            return true;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
