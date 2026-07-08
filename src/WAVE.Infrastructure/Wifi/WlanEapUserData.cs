using System.Runtime.InteropServices;
using WAVE.Application.Abstractions;

namespace WAVE.Infrastructure.Wifi;

/// <summary>
/// Aplica as credenciais de usuário (EAP) a um perfil WLAN Enterprise já criado,
/// via API nativa <c>wlanapi</c> (netsh não expõe esta operação). Best-effort: se
/// falhar, registra aviso e a conexão recai no prompt de credenciais do Windows.
/// </summary>
internal static class WlanEapUserData
{
    private const uint ClientVersion = 2;

    /// <summary>Aplica o XML de credenciais ao perfil na primeira interface Wi-Fi.</summary>
    public static bool TryApply(string profileName, string eapUserDataXml, IAppLogger logger)
    {
        var handle = IntPtr.Zero;
        var interfaceList = IntPtr.Zero;
        try
        {
            if (WlanOpenHandle(ClientVersion, IntPtr.Zero, out _, out handle) != 0)
            {
                logger.Warn("WlanOpenHandle falhou ao aplicar credenciais Enterprise.");
                return false;
            }

            if (WlanEnumInterfaces(handle, IntPtr.Zero, out interfaceList) != 0)
            {
                logger.Warn("WlanEnumInterfaces falhou ao aplicar credenciais Enterprise.");
                return false;
            }

            var count = Marshal.ReadInt32(interfaceList);
            if (count <= 0)
            {
                logger.Warn("Nenhuma interface Wi-Fi encontrada para credenciais Enterprise.");
                return false;
            }

            // Layout de WLAN_INTERFACE_INFO_LIST: dwNumberOfItems (4) + dwIndex (4),
            // seguidos do primeiro WLAN_INTERFACE_INFO cujo GUID vem no início.
            var interfaceGuid = Marshal.PtrToStructure<Guid>(interfaceList + 8);

            var status = WlanSetProfileEapXmlUserData(
                handle, ref interfaceGuid, profileName, 0, eapUserDataXml, IntPtr.Zero);

            if (status != 0)
            {
                logger.Warn($"WlanSetProfileEapXmlUserData retornou {status} para '{profileName}'.");
                return false;
            }

            return true;
        }
        catch (Exception exception)
        {
            logger.Error("Falha ao aplicar credenciais Enterprise (EAP).", exception);
            return false;
        }
        finally
        {
            if (interfaceList != IntPtr.Zero)
            {
                WlanFreeMemory(interfaceList);
            }

            if (handle != IntPtr.Zero)
            {
                WlanCloseHandle(handle, IntPtr.Zero);
            }
        }
    }

    [DllImport("wlanapi.dll")]
    private static extern uint WlanOpenHandle(
        uint dwClientVersion, IntPtr pReserved, out uint pdwNegotiatedVersion, out IntPtr phClientHandle);

    [DllImport("wlanapi.dll")]
    private static extern uint WlanEnumInterfaces(
        IntPtr hClientHandle, IntPtr pReserved, out IntPtr ppInterfaceList);

    [DllImport("wlanapi.dll", CharSet = CharSet.Unicode)]
    private static extern uint WlanSetProfileEapXmlUserData(
        IntPtr hClientHandle, ref Guid pInterfaceGuid, string strProfileName,
        uint dwFlags, string strEapXmlUserData, IntPtr pReserved);

    [DllImport("wlanapi.dll")]
    private static extern uint WlanCloseHandle(IntPtr hClientHandle, IntPtr pReserved);

    [DllImport("wlanapi.dll")]
    private static extern void WlanFreeMemory(IntPtr pMemory);
}
