using System.Windows.Controls;

namespace WAVE.App.Controls;

/// <summary>
/// Reusable network-button component. All behavior comes from the
/// <c>NetworkButtonViewModel</c> in its DataContext (no logic in the code-behind).
/// </summary>
public partial class NetworkButton : UserControl
{
    public NetworkButton() => InitializeComponent();
}
