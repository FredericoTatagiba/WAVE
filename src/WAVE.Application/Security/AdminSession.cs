using WAVE.Application.Abstractions;
using WAVE.Domain.Common;

namespace WAVE.Application.Security;

/// <summary>
/// Single administrator password, held in the settings as a PBKDF2 hash and unlocked for
/// the lifetime of the process once entered.
/// </summary>
public sealed class AdminSession : IAdminSession
{
    private const int MinPasswordLength = 8;

    private readonly ISettingsStore _settings;
    private readonly IPasswordHasher _hasher;

    private bool _unlocked;

    public AdminSession(ISettingsStore settings, IPasswordHasher hasher)
    {
        _settings = settings;
        _hasher = hasher;
    }

    public bool IsConfigured => !string.IsNullOrEmpty(_settings.Current.AdminPasswordHash);

    public bool IsUnlocked => _unlocked;

    public event EventHandler? Changed;

    public async Task<Result> ConfigureAsync(string password, CancellationToken cancellationToken = default)
    {
        if (IsConfigured)
        {
            return Result.Failure("Já existe uma senha de administrador neste dispositivo.");
        }

        var validation = Validate(password);
        if (validation.IsFailure)
        {
            return validation;
        }

        await StoreHashAsync(password, cancellationToken).ConfigureAwait(false);
        SetUnlocked(true);
        return Result.Success();
    }

    public Task<Result> UnlockAsync(string password, CancellationToken cancellationToken = default)
    {
        var hash = _settings.Current.AdminPasswordHash;
        if (string.IsNullOrEmpty(hash))
        {
            return Task.FromResult(Result.Failure("Nenhuma senha de administrador definida neste dispositivo."));
        }

        if (!_hasher.Verify(password ?? string.Empty, hash))
        {
            return Task.FromResult(Result.Failure("Senha incorreta."));
        }

        SetUnlocked(true);
        return Task.FromResult(Result.Success());
    }

    public async Task<Result> ChangePasswordAsync(
        string currentPassword, string newPassword, CancellationToken cancellationToken = default)
    {
        var unlocked = await UnlockAsync(currentPassword, cancellationToken).ConfigureAwait(false);
        if (unlocked.IsFailure)
        {
            return unlocked;
        }

        var validation = Validate(newPassword);
        if (validation.IsFailure)
        {
            return validation;
        }

        await StoreHashAsync(newPassword, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    public void Lock() => SetUnlocked(false);

    public Result RequireUnlocked() =>
        _unlocked
            ? Result.Success()
            : Result.Failure("Ação restrita ao administrador. Informe a senha para continuar.");

    private static Result Validate(string password) =>
        string.IsNullOrWhiteSpace(password) || password.Length < MinPasswordLength
            ? Result.Failure($"A senha deve ter ao menos {MinPasswordLength} caracteres.")
            : Result.Success();

    private async Task StoreHashAsync(string password, CancellationToken cancellationToken)
    {
        var updated = _settings.Current with { AdminPasswordHash = _hasher.Hash(password) };
        await _settings.SaveAsync(updated, cancellationToken).ConfigureAwait(false);
    }

    private void SetUnlocked(bool unlocked)
    {
        if (_unlocked == unlocked)
        {
            return;
        }

        _unlocked = unlocked;
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
