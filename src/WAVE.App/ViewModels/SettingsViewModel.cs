using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WAVE.Application.Abstractions;
using WAVE.Infrastructure.Configuration;

namespace WAVE.App.ViewModels;

/// <summary>
/// Device settings: where the audit artefacts are written, and the administrator password.
/// </summary>
public sealed class SettingsViewModel : ObservableObject
{
    private readonly ISettingsStore _settings;
    private readonly IAdminSession _admin;
    private readonly WaveDataPaths _paths;
    private readonly IAppLogger _logger;

    private string _historyDirectory = string.Empty;
    private string _logsDirectory = string.Empty;
    private string _status = string.Empty;

    public SettingsViewModel(
        ISettingsStore settings, IAdminSession admin, WaveDataPaths paths, IAppLogger logger)
    {
        _settings = settings;
        _admin = admin;
        _paths = paths;
        _logger = logger;

        _historyDirectory = settings.Current.HistoryDirectory ?? string.Empty;
        _logsDirectory = settings.Current.LogsDirectory ?? string.Empty;

        SaveCommand = new AsyncRelayCommand(SaveAsync);
    }

    /// <summary>Configured history directory; empty means the local default.</summary>
    public string HistoryDirectory
    {
        get => _historyDirectory;
        set => SetProperty(ref _historyDirectory, value);
    }

    /// <summary>Configured logs directory; empty means the local default.</summary>
    public string LogsDirectory
    {
        get => _logsDirectory;
        set => SetProperty(ref _logsDirectory, value);
    }

    /// <summary>
    /// Where the history is actually being written right now. Shown because a configured
    /// directory that cannot be reached falls back to the local one, and the operator
    /// should be able to see that rather than assume the share is receiving the records.
    /// </summary>
    public string EffectiveHistoryPath => _paths.HistoryFile;

    public string EffectiveLogsPath => _paths.LogsDirectory;

    public string Status
    {
        get => _status;
        private set
        {
            if (SetProperty(ref _status, value))
            {
                OnPropertyChanged(nameof(HasStatus));
            }
        }
    }

    public bool HasStatus => !string.IsNullOrEmpty(_status);

    public IAsyncRelayCommand SaveCommand { get; }

    private async Task SaveAsync()
    {
        try
        {
            var updated = _settings.Current with
            {
                HistoryDirectory = Normalize(HistoryDirectory),
                LogsDirectory = Normalize(LogsDirectory)
            };

            await _settings.SaveAsync(updated).ConfigureAwait(false);

            RefreshEffectivePaths();
            Status = "Configurações salvas.";
        }
        catch (Exception exception)
        {
            _logger.Error("Failed to save the settings.", exception);
            Status = "Falha ao salvar as configurações.";
        }
    }

    /// <summary>Replaces the administrator password. Requires the current one.</summary>
    public async Task ChangePasswordAsync(string currentPassword, string newPassword, string confirmPassword)
    {
        if (!string.Equals(newPassword, confirmPassword, StringComparison.Ordinal))
        {
            Status = "As senhas não coincidem.";
            return;
        }

        var result = await _admin.ChangePasswordAsync(currentPassword, newPassword).ConfigureAwait(false);
        Status = result.IsSuccess ? "Senha de administrador alterada." : result.Error;
    }

    private void RefreshEffectivePaths()
    {
        OnPropertyChanged(nameof(EffectiveHistoryPath));
        OnPropertyChanged(nameof(EffectiveLogsPath));
    }

    private static string? Normalize(string value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
