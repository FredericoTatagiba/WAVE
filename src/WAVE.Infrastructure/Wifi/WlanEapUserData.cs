using System.Runtime.InteropServices;
using WAVE.Application.Abstractions;

namespace WAVE.Infrastructure.Wifi;

/// <summary>
/// Applies the user credentials (EAP) to an already-created Enterprise WLAN profile,
/// via the native <c>wlanapi</c> API (netsh does not expose this operation). Best-effort:
/// if it fails, it logs a warning and the connection falls back to the Windows credential prompt.
/// </summary>
internal static class WlanEapUserData
{
    private const uint ClientVersion = 2;

    /// <summary>Applies the credentials XML to the profile on the first Wi-Fi interface.</summary>
    public static bool TryApply(string profileName, string eapUserDataXml, IAppLogger logger)
    {
        var handle = IntPtr.Zero;
        var interfaceList = IntPtr.Zero;
        try
        {
            if (WlanOpenHandle(ClientVersion, IntPtr.Zero, out _, out handle) != 0)
            {
                logger.Warn("WlanOpenHandle failed while applying Enterprise credentials.");
                return false;
            }

            if (WlanEnumInterfaces(handle, IntPtr.Zero, out interfaceList) != 0)
            {
                logger.Warn("WlanEnumInterfaces failed while applying Enterprise credentials.");
                return false;
            }

            var count = Marshal.ReadInt32(interfaceList);
            if (count <= 0)
            {
                logger.Warn("No Wi-Fi interface found for Enterprise credentials.");
                return false;
            }

            // WLAN_INTERFACE_INFO_LIST layout: dwNumberOfItems (4) + dwIndex (4),
            // followed by the first WLAN_INTERFACE_INFO whose GUID comes at the start.
            var interfaceGuid = Marshal.PtrToStructure<Guid>(interfaceList + 8);

            var status = WlanSetProfileEapXmlUserData(
                handle, ref interfaceGuid, profileName, 0, eapUserDataXml, IntPtr.Zero);

            if (status != 0)
            {
                logger.Warn($"WlanSetProfileEapXmlUserData returned {status} for '{profileName}'.");
                return false;
            }

            return true;
        }
        catch (Exception exception)
        {
            logger.Error("Failed to apply Enterprise credentials (EAP).", exception);
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
                _ = WlanCloseHandle(handle, IntPtr.Zero);
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
