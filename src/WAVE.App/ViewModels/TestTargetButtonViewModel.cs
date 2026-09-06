using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WAVE.Domain.Testing;

namespace WAVE.App.ViewModels;

/// <summary>
/// Shared behaviour of the buttons that start a test: the label lines, the state colour,
/// the busy lock and the run command. A Wi-Fi network and the wired adapter differ only
/// in what they are labelled with and what running them does, so both bind to the same
/// <see cref="Controls.NetworkButton"/> control through this contract.
/// </summary>
public abstract class TestTargetButtonViewModel : ObservableObject
{
    private TestOperationState _state = TestOperationState.Idle;
    private bool _isEnabled = true;

    protected TestTargetButtonViewModel(string displayName, string subtitle, string info, Func<Task> onRun)
    {
        DisplayName = displayName;
        Subtitle = subtitle;
        Info = info;
        RunCommand = new AsyncRelayCommand(onRun, () => IsEnabled && IsAvailable);
    }

    /// <summary>Identifier the orchestrator reports while this target is the one under test.</summary>
    public abstract string TargetKey { get; }

    public string DisplayName { get; }

    /// <summary>Second line: the SSID, or the wired adapter's name.</summary>
    public string Subtitle { get; }

    /// <summary>Auxiliary line: security, readiness and signal, or the link state.</summary>
    public string Info { get; }

    /// <summary>The target exists and can be tested at all.</summary>
    public virtual bool IsAvailable => true;

    public TestOperationState State
    {
        get => _state;
        set
        {
            if (SetProperty(ref _state, value))
            {
                OnPropertyChanged(nameof(IsConnecting));
            }
        }
    }

    /// <summary>This target is the one currently being connected to.</summary>
    public bool IsConnecting => _state == TestOperationState.Connecting;

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
