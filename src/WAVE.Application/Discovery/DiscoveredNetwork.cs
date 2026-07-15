using WAVE.Domain.Networking;

namespace WAVE.Application.Discovery;

/// <summary>
/// Network ready to become a test button. <see cref="ReadyToConnect"/> indicates that
/// no password needs to be typed (open network, saved in Windows or already registered).
/// </summary>
public sealed record DiscoveredNetwork(WifiNetworkProfile Profile, bool ReadyToConnect, int SignalPercent);
