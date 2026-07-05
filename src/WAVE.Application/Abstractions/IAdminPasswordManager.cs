using WAVE.Domain.Common;

namespace WAVE.Application.Abstractions;

/// <summary>Define/atualiza a senha administrativa (armazenada como hash forte).</summary>
public interface IAdminPasswordManager
{
    bool IsConfigured { get; }

    /// <summary>Define a senha inicial. Falha se já houver uma configurada.</summary>
    Result SetInitialPassword(string password);

    /// <summary>Troca a senha mediante validação da senha atual.</summary>
    Result ChangePassword(string currentPassword, string newPassword);
}
