using WAVE.Domain.Networking;

namespace WAVE.Application.Networking;

/// <summary>
/// Parses the terse (<c>-t</c>) output of NetworkManager's <c>nmcli</c>. Pure text logic,
/// kept in the Application layer so it is testable without spawning a process — the same
/// split the netsh parsing already follows.
/// </summary>
/// <remarks>
/// Terse mode escapes the field separator as <c>\:</c> and a literal backslash as
/// <c>\\</c>. Splitting naively on ':' therefore corrupts any SSID containing a colon,
/// which is legal and does occur.
/// </remarks>
public static class NmcliOutputParser
{
    /// <summary>
    /// Parses <c>nmcli -t -f SSID,SECURITY,SIGNAL device wifi list</c>.
    /// </summary>
    /// <remarks>
    /// Unlike netsh, nmcli emits one row per BSS, so a dual-band AP appears twice. Rows
    /// are collapsed to one entry per SSID keeping the strongest signal, otherwise the UI
    /// renders duplicate buttons for the same network.
    /// </remarks>
    public static IReadOnlyList<AvailableNetwork> ParseScan(string standardOutput)
    {
        var strongest = new Dictionary<string, AvailableNetwork>(StringComparer.Ordinal);

        foreach (var line in SplitLines(standardOutput))
        {
            var fields = SplitFields(line);
            if (fields.Count < 3)
            {
                continue;
            }

            var ssid = fields[0].Trim();
            if (ssid.Length == 0)
            {
                // Hidden network: no SSID to connect to or label a button with.
                continue;
            }

            var network = new AvailableNetwork(
                ssid,
                WifiSecurityParser.FromSecurityText(fields[1]),
                ParseSignal(fields[2]));

            if (!strongest.TryGetValue(ssid, out var existing) ||
                network.SignalPercent > existing.SignalPercent)
            {
                strongest[ssid] = network;
            }
        }

        return strongest.Values.ToList();
    }

    /// <summary>
    /// Parses <c>nmcli -t -f NAME,TYPE connection show</c>, keeping only Wi-Fi
    /// connections. The saved-profile list must not include ethernet, bridges or VPNs,
    /// or the app would report a wired connection as a known Wi-Fi network.
    /// </summary>
    public static IReadOnlyList<string> ParseConnectionNames(string standardOutput)
    {
        const string WifiConnectionType = "802-11-wireless";

        var names = new List<string>();

        foreach (var line in SplitLines(standardOutput))
        {
            var fields = SplitFields(line);
            if (fields.Count < 2 ||
                !fields[1].Trim().Equals(WifiConnectionType, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var name = fields[0].Trim();
            if (name.Length > 0)
            {
                names.Add(name);
            }
        }

        return names;
    }

    /// <summary>
    /// Parses <c>nmcli -t -f DEVICE,TYPE device status</c> and returns the first Wi-Fi
    /// interface name, or null when the machine has none.
    /// </summary>
    /// <remarks>
    /// Asking nmcli rather than <c>NetworkInterface.NetworkInterfaceType</c> because .NET
    /// on Linux frequently reports a wlan adapter as <c>Ethernet</c>.
    /// </remarks>
    public static string? ParseFirstWifiDevice(string standardOutput)
    {
        foreach (var line in SplitLines(standardOutput))
        {
            var fields = SplitFields(line);
            if (fields.Count >= 2 &&
                fields[1].Trim().Equals("wifi", StringComparison.OrdinalIgnoreCase))
            {
                var device = fields[0].Trim();
                if (device.Length > 0)
                {
                    return device;
                }
            }
        }

        return null;
    }

    private static IEnumerable<string> SplitLines(string standardOutput) =>
        string.IsNullOrEmpty(standardOutput)
            ? []
            : standardOutput
                .Replace("\r\n", "\n", StringComparison.Ordinal)
                .Split('\n')
                .Where(line => line.Length > 0);

    /// <summary>
    /// Splits one terse line on unescaped ':' separators and unescapes each field.
    /// </summary>
    private static List<string> SplitFields(string line)
    {
        var fields = new List<string>();
        var current = new System.Text.StringBuilder();

        for (var index = 0; index < line.Length; index++)
        {
            var character = line[index];

            if (character == '\\' && index + 1 < line.Length)
            {
                // An escape consumes the next character verbatim, so "\:" yields a colon
                // inside the field instead of ending it.
                current.Append(line[++index]);
                continue;
            }

            if (character == ':')
            {
                fields.Add(current.ToString());
                current.Clear();
                continue;
            }

            current.Append(character);
        }

        fields.Add(current.ToString());
        return fields;
    }

    private static int ParseSignal(string text) =>
        int.TryParse(text.Trim(), out var percent) ? Math.Clamp(percent, 0, 100) : 0;
}
