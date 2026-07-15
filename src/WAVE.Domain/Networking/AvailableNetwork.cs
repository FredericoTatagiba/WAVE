namespace WAVE.Domain.Networking;

/// <summary>Wi-Fi network detected during the scan (name, security and approximate signal).</summary>
public sealed record AvailableNetwork(string Ssid, SecurityType Security, int SignalPercent);
