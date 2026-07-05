using WAVE.Domain.Networking;

namespace WAVE.Application.Discovery;

/// <summary>
/// Rede pronta para virar botão de teste. <see cref="ReadyToConnect"/> indica que
/// não é preciso digitar senha (rede aberta, salva no Windows ou já cadastrada).
/// </summary>
public sealed record DiscoveredNetwork(WifiNetworkProfile Profile, bool ReadyToConnect, int SignalPercent);
