using System.Windows;
using System.Windows.Controls;

namespace WAVE.App.Controls;

/// <summary>
/// Componente de layout que exibe dois conteúdos lado a lado em paisagem
/// (lista à esquerda, gráficos à direita) e empilhados em retrato (lista em cima,
/// gráficos abaixo), conforme a regra de interface do projeto.
/// </summary>
public partial class ResponsiveSplitView : UserControl
{
    public static readonly DependencyProperty PrimaryContentProperty = DependencyProperty.Register(
        nameof(PrimaryContent), typeof(object), typeof(ResponsiveSplitView), new PropertyMetadata(null));

    public static readonly DependencyProperty SecondaryContentProperty = DependencyProperty.Register(
        nameof(SecondaryContent), typeof(object), typeof(ResponsiveSplitView), new PropertyMetadata(null));

    public ResponsiveSplitView()
    {
        InitializeComponent();
        Loaded += (_, _) => ApplyOrientation();
        SizeChanged += (_, _) => ApplyOrientation();
    }

    public object? PrimaryContent
    {
        get => GetValue(PrimaryContentProperty);
        set => SetValue(PrimaryContentProperty, value);
    }

    public object? SecondaryContent
    {
        get => GetValue(SecondaryContentProperty);
        set => SetValue(SecondaryContentProperty, value);
    }

    private void ApplyOrientation()
    {
        var isLandscape = ActualWidth >= ActualHeight;

        RootGrid.RowDefinitions.Clear();
        RootGrid.ColumnDefinitions.Clear();

        if (isLandscape)
        {
            RootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            RootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });

            Grid.SetRow(PrimaryHost, 0);
            Grid.SetColumn(PrimaryHost, 0);
            Grid.SetRow(SecondaryHost, 0);
            Grid.SetColumn(SecondaryHost, 1);
        }
        else
        {
            RootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            RootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            Grid.SetColumn(PrimaryHost, 0);
            Grid.SetRow(PrimaryHost, 0);
            Grid.SetColumn(SecondaryHost, 0);
            Grid.SetRow(SecondaryHost, 1);
        }
    }
}
