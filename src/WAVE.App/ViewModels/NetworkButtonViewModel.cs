using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WAVE.Domain.Networking;
using WAVE.Domain.Testing;

namespace WAVE.App.ViewModels;

/// <summary>ViewModel for a network button (reusable list component).</summary>
public sealed class NetworkButtonViewModel : ObservableObject
{
    private TestOperationState _state = TestOperationState.Idle;
    private bool _isEnabled = true;

    public NetworkButtonViewModel(
        WifiNetworkProfile profile, string info, bool readyToConnect, Func<NetworkButtonViewModel, Task> onRun)
    {
        Profile = profile;
        Info = info;
        ReadyToConnect = readyToConnect;
        RunCommand = new AsyncRelayCommand(() => onRun(this), () => IsEnabled);
    }

    public WifiNetworkProfile Profile { get; }

    public string DisplayName => Profile.DisplayName;

    public string Ssid => Profile.Ssid;

    /// <summary>
    /// Network already known to the system (open, saved in Windows or already registered):
    /// there is no need to ask for the password when selecting it.
    /// </summary>
    public bool ReadyToConnect { get; }

    /// <summary>Auxiliary line (security, readiness and signal).</summary>
    public string Info { get; }

    public TestOperationState State
    {
        get => _state;
        set => SetProperty(ref _state, value);
    }

    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            if (SetProperty(ref _isEnabled, value))
            {
                RunCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public IAsyncRelayCommand RunCommand { get; }
}
