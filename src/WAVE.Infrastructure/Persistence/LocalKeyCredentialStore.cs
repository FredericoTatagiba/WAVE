using System.Security.Cryptography;
using WAVE.Application.Abstractions;
using WAVE.Infrastructure.Configuration;

namespace WAVE.Infrastructure.Persistence;

/// <summary>
/// Stores network secrets encrypted with AES-GCM, keyed by a random 256-bit key held in
/// a 0600 file next to the store. The DPAPI equivalent for platforms without DPAPI.
/// </summary>
/// <remarks>
/// libsecret was the obvious alternative and was rejected: it needs a live keyring daemon
/// on a D-Bus session, and this tool is expected to run over SSH and on kiosks, where that
/// store fails silently. Keying off the operator's own login password was also rejected —
/// it would make saved networks per-operator, a functional regression against Windows.
/// </remarks>
public sealed class LocalKeyCredentialStore : FileCredentialStore
{
    private const int KeySizeBytes = 32;
    private const int NonceSizeBytes = 12;
    private const int TagSizeBytes = 16;

    private readonly Lazy<byte[]> _key;

    public LocalKeyCredentialStore(IAppLogger logger)
        : base(logger) =>
        // ponytail: the key sits beside the ciphertext, so protection is the 0600 mode —
        // in practice equal to DPAPI-CurrentUser, which also falls to anyone who can read
        // as this OS user. Move to libsecret only if a desktop session is guaranteed.
        _key = new Lazy<byte[]>(LoadOrCreateKey);

    protected override byte[] Protect(byte[] plaintext)
    {
        var nonce = RandomNumberGenerator.GetBytes(NonceSizeBytes);
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[TagSizeBytes];

        using (var aes = new AesGcm(_key.Value, TagSizeBytes))
        {
            aes.Encrypt(nonce, plaintext, ciphertext, tag);
        }

        // Layout: nonce || tag || ciphertext
        var blob = new byte[NonceSizeBytes + TagSizeBytes + ciphertext.Length];
        nonce.CopyTo(blob, 0);
        tag.CopyTo(blob, NonceSizeBytes);
        ciphertext.CopyTo(blob, NonceSizeBytes + TagSizeBytes);
        return blob;
    }

    protected override byte[] Unprotect(byte[] ciphertext)
    {
        ArgumentNullException.ThrowIfNull(ciphertext);

        if (ciphertext.Length < NonceSizeBytes + TagSizeBytes)
        {
            throw new CryptographicException("Credential blob is truncated.");
        }

        var nonce = ciphertext.AsSpan(0, NonceSizeBytes);
        var tag = ciphertext.AsSpan(NonceSizeBytes, TagSizeBytes);
        var payload = ciphertext.AsSpan(NonceSizeBytes + TagSizeBytes);
        var plaintext = new byte[payload.Length];

        using var aes = new AesGcm(_key.Value, TagSizeBytes);

        // Throws CryptographicException on a tampered blob or a mismatched key; the base
        // class turns that into "no credential" plus a log entry.
        aes.Decrypt(nonce, payload, tag, plaintext);
        return plaintext;
    }

    private static byte[] LoadOrCreateKey()
    {
        var path = AppPaths.CredentialsKeyFile;

        if (File.Exists(path))
        {
            var existing = File.ReadAllBytes(path);
            if (existing.Length == KeySizeBytes)
            {
                return existing;
            }

            // A truncated key would fail every decrypt anyway; regenerating at least makes
            // new saves work again. The old ciphertexts are unrecoverable either way.
            throw new CryptographicException(
                $"Credential key at '{path}' is {existing.Length} bytes, expected {KeySizeBytes}. " +
                "Delete it to start over; saved Wi-Fi passwords will need re-entering.");
        }

        var key = RandomNumberGenerator.GetBytes(KeySizeBytes);
        File.WriteAllBytes(path, key);
        SecureFile.RestrictToOwner(path);
        return key;
    }
}
