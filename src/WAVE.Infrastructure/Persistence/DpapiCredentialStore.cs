using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
using WAVE.Application.Abstractions;

namespace WAVE.Infrastructure.Persistence;

/// <summary>
/// Stores network secrets encrypted with DPAPI (current-user scope).
/// The secrets are never written in clear text.
/// </summary>
/// <remarks>
/// Windows only — <c>ProtectedData</c> throws on every other platform. The composition
/// root selects <see cref="LocalKeyCredentialStore"/> elsewhere. Blobs are not portable
/// between accounts or machines by design; a store copied across simply fails to decrypt
/// and the operator re-enters the passphrase once.
/// </remarks>
[SupportedOSPlatform("windows")]
public sealed class DpapiCredentialStore : FileCredentialStore
{
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("WAVE.Credential.v1");

    public DpapiCredentialStore(IAppLogger logger)
        : base(logger)
    {
    }

    protected override byte[] Protect(byte[] plaintext) =>
        ProtectedData.Protect(plaintext, Entropy, DataProtectionScope.CurrentUser);

    protected override byte[] Unprotect(byte[] ciphertext) =>
        ProtectedData.Unprotect(ciphertext, Entropy, DataProtectionScope.CurrentUser);
}
