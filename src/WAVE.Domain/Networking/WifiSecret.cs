namespace WAVE.Domain.Networking;

/// <summary>
/// Segredo de acesso a uma rede. Para Personal, usa-se <see cref="Passphrase"/>.
/// Para Enterprise, também <see cref="Username"/>/<see cref="Domain"/>.
/// Nunca é serializado em texto claro (ver ICredentialStore/DPAPI).
/// </summary>
public sealed record WifiSecret(string Passphrase, string? Username = null, string? Domain = null);
