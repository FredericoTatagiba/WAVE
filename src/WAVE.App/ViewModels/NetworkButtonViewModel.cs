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
    /// Rede já conhecida pelo sistema (aberta, salva no Windows ou já cadastrada):
    /// não é preciso pedir a senha ao selecioná-la.
    /// </summary>
    public bool ReadyToConnect { get; }

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
