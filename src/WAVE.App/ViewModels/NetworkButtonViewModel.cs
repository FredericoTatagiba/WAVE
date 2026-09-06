using WAVE.Domain.Networking;

namespace WAVE.App.ViewModels;

/// <summary>ViewModel for a Wi-Fi network button (reusable list component).</summary>
public sealed class NetworkButtonViewModel : TestTargetButtonViewModel
{
    public NetworkButtonViewModel(
        WifiNetworkProfile profile, string info, bool readyToConnect, Func<Task> onRun)
        : base(profile.DisplayName, profile.Ssid, info, onRun)
    {
        Profile = profile;
        ReadyToConnect = readyToConnect;
    }

    public WifiNetworkProfile Profile { get; }

    public override string TargetKey => Profile.Ssid;

    /// <summary>
    /// Network already known to the system (open, saved in Windows or already registered):
    /// there is no need to ask for the password when selecting it.
    /// </summary>
    public bool ReadyToConnect { get; }
}
