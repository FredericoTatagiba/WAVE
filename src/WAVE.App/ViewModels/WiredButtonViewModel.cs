using System.Globalization;
using WAVE.Domain.Networking;

namespace WAVE.App.ViewModels;

/// <summary>
/// ViewModel for the wired-network button. There is only ever one, and it is rebuilt on
/// every scan so unplugging the cable is reflected in its label.
/// </summary>
public sealed class WiredButtonViewModel : TestTargetButtonViewModel
{
    private readonly EthernetLink? _link;

    public WiredButtonViewModel(EthernetLink? link, Func<Task> onRun)
        : base("Cabo de rede", SubtitleFor(link), InfoFor(link), onRun) => _link = link;

    public override string TargetKey => _link?.InterfaceName ?? string.Empty;

    public override bool IsAvailable => _link is not null;

    private static string SubtitleFor(EthernetLink? link) => link?.Description ?? "—";

    private static string InfoFor(EthernetLink? link)
    {
        if (link is null)
        {
            return "Nenhum adaptador cabeado";
        }

        if (!link.IsUp)
        {
            return "Cabo desconectado";
        }

        var address = link.HasDhcpLease ? "com IP" : "sem IP";

        return link.SpeedMbps > 0
            ? $"Link {link.SpeedMbps.ToString(CultureInfo.CurrentCulture)} Mbps · {address}"
            : $"Link ativo · {address}";
    }
}
