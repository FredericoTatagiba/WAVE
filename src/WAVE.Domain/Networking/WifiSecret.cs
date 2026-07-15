namespace WAVE.Domain.Networking;

/// <summary>
/// Access secret for a network. For Personal, <see cref="Passphrase"/> is used.
/// For Enterprise, also <see cref="Username"/>/<see cref="Domain"/>.
/// Never serialized in clear text (see ICredentialStore/DPAPI).
/// </summary>
public sealed record WifiSecret(string Passphrase, string? Username = null, string? Domain = null);
