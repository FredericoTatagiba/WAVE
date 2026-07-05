using System.Windows.Controls;

namespace WAVE.App.Controls;

/// <summary>
/// Componente reutilizável de botão de rede. Todo o comportamento vem do
/// <c>NetworkButtonViewModel</c> em seu DataContext (sem lógica no code-behind).
/// </summary>
public partial class NetworkButton : UserControl
{
    public NetworkButton() => InitializeComponent();
}
