namespace WAVE.Domain.Networking;

/// <summary>
/// Snapshot of the wired adapter WAVE tests over. Unlike Wi-Fi there is nothing to
/// associate with: the cable is either carrying a link or it is not, so the state the
/// test needs is the adapter's own.
/// </summary>
/// <param name="InterfaceName">Adapter identifier used to match history and UI state.</param>
/// <param name="Description">Human-facing adapter name shown to the operator.</param>
/// <param name="IsUp">The adapter reports an active link (cable plugged in).</param>
/// <param name="SpeedMbps">Negotiated link speed, or 0 when the adapter does not report one.</param>
/// <param name="HasDhcpLease">The adapter holds a routable IPv4 address and a gateway.</param>
public sealed record EthernetLink(
    string InterfaceName,
    string Description,
    bool IsUp,
    long SpeedMbps,
    bool HasDhcpLease);
