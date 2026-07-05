using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using WAVE.Application.Abstractions;
using WAVE.Domain.Common;
using WAVE.Infrastructure.Configuration;

namespace WAVE.Infrastructure.Security;

/// <summary>
/// Gerencia e verifica a senha administrativa usando PBKDF2 (SHA-256) com sal
/// aleatório. A senha em si nunca é persistida — apenas o hash.
/// Formato do arquivo: <c>iterations:saltBase64:hashBase64</c>.
/// </summary>
public sealed class Pbkdf2AdminPasswordManager : IAdminPasswordVerifier, IAdminPasswordManager
{
    private const int Iterations = 120_000;
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const char Separator = ':';

    private readonly IAppLogger _logger;
    private readonly string _file;

    public Pbkdf2AdminPasswordManager(IAppLogger logger)
    {
        _logger = logger;
        AppPaths.EnsureCreated();
        _file = AppPaths.AdminHashFile;
    }

    public bool IsConfigured => File.Exists(_file);

    public bool Verify(string password)
    {
        if (!IsConfigured || string.IsNullOrEmpty(password))
        {
            return false;
        }

        try
        {
            var parts = File.ReadAllText(_file).Trim().Split(Separator);
            if (parts.Length != 3)
            {
                return false;
            }

            var iterations = int.Parse(parts[0], CultureInfo.InvariantCulture);
            var salt = Convert.FromBase64String(parts[1]);
            var expected = Convert.FromBase64String(parts[2]);

            var actual = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(password), salt, iterations, HashAlgorithmName.SHA256, expected.Length);

            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch (Exception exception)
        {
            _logger.Error("Falha ao verificar a senha administrativa.", exception);
            return false;
        }
    }

    public Result SetInitialPassword(string password)
    {
        if (IsConfigured)
        {
            return Result.Failure("Já existe uma senha administrativa configurada.");
        }

        return WritePassword(password);
    }

    public Result ChangePassword(string currentPassword, string newPassword)
    {
        if (IsConfigured && !Verify(currentPassword))
        {
            return Result.Failure("Senha atual inválida.");
        }

        return WritePassword(newPassword);
    }

    private Result WritePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
        {
            return Result.Failure("A senha deve ter ao menos 8 caracteres.");
        }

        try
        {
            var salt = RandomNumberGenerator.GetBytes(SaltSize);
            var hash = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(password), salt, Iterations, HashAlgorithmName.SHA256, HashSize);

            var content = string.Join(
                Separator,
                Iterations.ToString(CultureInfo.InvariantCulture),
                Convert.ToBase64String(salt),
                Convert.ToBase64String(hash));

            File.WriteAllText(_file, content);
            return Result.Success();
        }
        catch (Exception exception)
        {
            _logger.Error("Falha ao gravar a senha administrativa.", exception);
            return Result.Failure("Não foi possível gravar a senha administrativa.");
        }
    }
}
