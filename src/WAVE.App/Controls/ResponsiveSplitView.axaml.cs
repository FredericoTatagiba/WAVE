using Avalonia;
using Avalonia.Controls;

namespace WAVE.App.Controls;

/// <summary>
/// Layout component that arranges two contents either side by side or stacked, following
/// the project's responsive interface rule. Two decision strategies:
/// <list type="bullet">
///   <item>Orientation (default): side by side in landscape, stacked in portrait.</item>
///   <item>Width breakpoint: set <see cref="StackBelowWidth"/> to stack once the available
///   width drops below that threshold — used for a chart + accessory pair that is wide and
///   short, where the orientation heuristic would never flip.</item>
/// </list>
/// The side-by-side and stacked proportions are configurable; the defaults preserve the
/// original list/telemetry behavior.
/// </summary>
public partial class ResponsiveSplitView : UserControl
{
    public static readonly StyledProperty<object?> PrimaryContentProperty =
        AvaloniaProperty.Register<ResponsiveSplitView, object?>(nameof(PrimaryContent));

    public static readonly StyledProperty<object?> SecondaryContentProperty =
        AvaloniaProperty.Register<ResponsiveSplitView, object?>(nameof(SecondaryContent));

    /// <summary>When &gt; 0, stacks once the rendered width is below this value; otherwise uses orientation.</summary>
    public static readonly StyledProperty<double> StackBelowWidthProperty =
        AvaloniaProperty.Register<ResponsiveSplitView, double>(nameof(StackBelowWidth));

    /// <summary>Primary length when side by side (column). Default 1*.</summary>
    public static readonly StyledProperty<GridLength> PrimaryLengthProperty =
        AvaloniaProperty.Register<ResponsiveSplitView, GridLength>(
            nameof(PrimaryLength), new GridLength(1, GridUnitType.Star));

    /// <summary>Secondary length when side by side (column). Default 2*.</summary>
    public static readonly StyledProperty<GridLength> SecondaryLengthProperty =
        AvaloniaProperty.Register<ResponsiveSplitView, GridLength>(
            nameof(SecondaryLength), new GridLength(2, GridUnitType.Star));

    /// <summary>Primary length when stacked (row). Default 1*.</summary>
    public static readonly StyledProperty<GridLength> StackedPrimaryLengthProperty =
        AvaloniaProperty.Register<ResponsiveSplitView, GridLength>(
            nameof(StackedPrimaryLength), new GridLength(1, GridUnitType.Star));

    /// <summary>Secondary length when stacked (row). Default 1*.</summary>
    public static readonly StyledProperty<GridLength> StackedSecondaryLengthProperty =
        AvaloniaProperty.Register<ResponsiveSplitView, GridLength>(
            nameof(StackedSecondaryLength), new GridLength(1, GridUnitType.Star));

    public ResponsiveSplitView()
    {
        InitializeComponent();
        Loaded += (_, _) => ApplyLayout();
        SizeChanged += (_, _) => ApplyLayout();
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

    public double StackBelowWidth
    {
        get => GetValue(StackBelowWidthProperty);
        set => SetValue(StackBelowWidthProperty, value);
    }

    public GridLength PrimaryLength
    {
        get => GetValue(PrimaryLengthProperty);
        set => SetValue(PrimaryLengthProperty, value);
    }

    public GridLength SecondaryLength
    {
        get => GetValue(SecondaryLengthProperty);
        set => SetValue(SecondaryLengthProperty, value);
    }

    public GridLength StackedPrimaryLength
    {
        get => GetValue(StackedPrimaryLengthProperty);
        set => SetValue(StackedPrimaryLengthProperty, value);
    }

    public GridLength StackedSecondaryLength
    {
        get => GetValue(StackedSecondaryLengthProperty);
        set => SetValue(StackedSecondaryLengthProperty, value);
    }

    /// <summary>
    /// Re-runs the layout when any of the sizing inputs change. Replaces the five
    /// PropertyMetadata callbacks WPF needed.
    /// </summary>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == StackBelowWidthProperty ||
            change.Property == PrimaryLengthProperty ||
            change.Property == SecondaryLengthProperty ||
            change.Property == StackedPrimaryLengthProperty ||
            change.Property == StackedSecondaryLengthProperty)
        {
            ApplyLayout();
        }
    }

    private void ApplyLayout()
    {
        // Avalonia exposes the rendered size as Bounds; there is no ActualWidth/Height.
        var stacked = StackBelowWidth > 0
            ? Bounds.Width < StackBelowWidth
            : Bounds.Width < Bounds.Height;

        RootGrid.RowDefinitions.Clear();
        RootGrid.ColumnDefinitions.Clear();

        if (!stacked)
        {
            RootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = PrimaryLength });
            RootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = SecondaryLength });

            Grid.SetRow(PrimaryHost, 0);
            Grid.SetColumn(PrimaryHost, 0);
            Grid.SetRow(SecondaryHost, 0);
            Grid.SetColumn(SecondaryHost, 1);
        }
        else
        {
            RootGrid.RowDefinitions.Add(new RowDefinition { Height = StackedPrimaryLength });
            RootGrid.RowDefinitions.Add(new RowDefinition { Height = StackedSecondaryLength });

            Grid.SetColumn(PrimaryHost, 0);
            Grid.SetRow(PrimaryHost, 0);
            Grid.SetColumn(SecondaryHost, 0);
            Grid.SetRow(SecondaryHost, 1);
        }
    }
}
