namespace WAVE.Domain.Networking;

/// <summary>Rede Wi-Fi detectada na varredura (nome, segurança e sinal aproximado).</summary>
public sealed record AvailableNetwork(string Ssid, SecurityType Security, int SignalPercent);
