using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WAVE.Domain.Networking;
using WAVE.Domain.Testing;

namespace WAVE.App.ViewModels;

/// <summary>ViewModel de um botão de rede (componente reutilizável da lista).</summary>
public sealed class NetworkButtonViewModel : ObservableObject
{
    private TestOperationState _state = TestOperationState.Idle;
    private bool _isEnabled = true;

    public NetworkButtonViewModel(WifiNetworkProfile profile, string info, Func<NetworkButtonViewModel, Task> onRun)
    {
        Profile = profile;
        Info = info;
        RunCommand = new AsyncRelayCommand(() => onRun(this), () => IsEnabled);
    }

    public WifiNetworkProfile Profile { get; }

    public string DisplayName => Profile.DisplayName;

    public string Ssid => Profile.Ssid;

    /// <summary>Linha auxiliar (segurança, prontidão e sinal).</summary>
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
