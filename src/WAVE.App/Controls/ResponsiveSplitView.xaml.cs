using System.Windows;
using System.Windows.Controls;

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
    public static readonly DependencyProperty PrimaryContentProperty = DependencyProperty.Register(
        nameof(PrimaryContent), typeof(object), typeof(ResponsiveSplitView), new PropertyMetadata(null));

    public static readonly DependencyProperty SecondaryContentProperty = DependencyProperty.Register(
        nameof(SecondaryContent), typeof(object), typeof(ResponsiveSplitView), new PropertyMetadata(null));

    /// <summary>When &gt; 0, stacks once <see cref="UserControl.ActualWidth"/> is below this value; otherwise uses orientation.</summary>
    public static readonly DependencyProperty StackBelowWidthProperty = DependencyProperty.Register(
        nameof(StackBelowWidth), typeof(double), typeof(ResponsiveSplitView),
        new PropertyMetadata(0d, OnLayoutInputChanged));

    /// <summary>Primary length when side by side (column). Default 1*.</summary>
    public static readonly DependencyProperty PrimaryLengthProperty = DependencyProperty.Register(
        nameof(PrimaryLength), typeof(GridLength), typeof(ResponsiveSplitView),
        new PropertyMetadata(new GridLength(1, GridUnitType.Star), OnLayoutInputChanged));

    /// <summary>Secondary length when side by side (column). Default 2*.</summary>
    public static readonly DependencyProperty SecondaryLengthProperty = DependencyProperty.Register(
        nameof(SecondaryLength), typeof(GridLength), typeof(ResponsiveSplitView),
        new PropertyMetadata(new GridLength(2, GridUnitType.Star), OnLayoutInputChanged));

    /// <summary>Primary length when stacked (row). Default 1*.</summary>
    public static readonly DependencyProperty StackedPrimaryLengthProperty = DependencyProperty.Register(
        nameof(StackedPrimaryLength), typeof(GridLength), typeof(ResponsiveSplitView),
        new PropertyMetadata(new GridLength(1, GridUnitType.Star), OnLayoutInputChanged));

    /// <summary>Secondary length when stacked (row). Default 1*.</summary>
    public static readonly DependencyProperty StackedSecondaryLengthProperty = DependencyProperty.Register(
        nameof(StackedSecondaryLength), typeof(GridLength), typeof(ResponsiveSplitView),
        new PropertyMetadata(new GridLength(1, GridUnitType.Star), OnLayoutInputChanged));

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
        get => (double)GetValue(StackBelowWidthProperty);
        set => SetValue(StackBelowWidthProperty, value);
    }

    public GridLength PrimaryLength
    {
        get => (GridLength)GetValue(PrimaryLengthProperty);
        set => SetValue(PrimaryLengthProperty, value);
    }

    public GridLength SecondaryLength
    {
        get => (GridLength)GetValue(SecondaryLengthProperty);
        set => SetValue(SecondaryLengthProperty, value);
    }

    public GridLength StackedPrimaryLength
    {
        get => (GridLength)GetValue(StackedPrimaryLengthProperty);
        set => SetValue(StackedPrimaryLengthProperty, value);
    }

    public GridLength StackedSecondaryLength
    {
        get => (GridLength)GetValue(StackedSecondaryLengthProperty);
        set => SetValue(StackedSecondaryLengthProperty, value);
    }

    private static void OnLayoutInputChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((ResponsiveSplitView)d).ApplyLayout();

    private void ApplyLayout()
    {
        var stacked = StackBelowWidth > 0
            ? ActualWidth < StackBelowWidth
            : ActualWidth < ActualHeight;

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
