using System.Collections;
using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;

namespace WAVE.App.Controls;

/// <summary>
/// Live latency chart component. Draws a polyline from a collection of values (ms)
/// and redraws when the collection or the size changes.
/// No external charting-library dependencies.
/// </summary>
public partial class PingLatencyChart : UserControl
{
    private const double MinimumScaleMs = 50d;

    private static readonly IBrush LineBrush = new SolidColorBrush(Color.FromRgb(0x4C, 0xC2, 0xFF));

    public static readonly StyledProperty<IEnumerable?> ItemsSourceProperty =
        AvaloniaProperty.Register<PingLatencyChart, IEnumerable?>(nameof(ItemsSource));

    private INotifyCollectionChanged? _observable;

    public PingLatencyChart()
    {
        InitializeComponent();
        SizeChanged += (_, _) => Redraw();
    }

    public IEnumerable? ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    /// <summary>
    /// Resubscribes to the new collection. Replaces WPF's PropertyMetadata callback;
    /// Avalonia routes styled-property changes through this single override.
    /// </summary>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property != ItemsSourceProperty)
        {
            return;
        }

        if (_observable is not null)
        {
            _observable.CollectionChanged -= OnCollectionChanged;
        }

        _observable = change.NewValue as INotifyCollectionChanged;

        if (_observable is not null)
        {
            _observable.CollectionChanged += OnCollectionChanged;
        }

        Redraw();
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => Redraw();

    private void Redraw()
    {
        PlotCanvas.Children.Clear();

        var values = ReadValues();
        EmptyLabel.IsVisible = values.Count == 0;

        // Avalonia exposes the rendered size as Bounds; there is no ActualWidth/Height.
        var width = PlotCanvas.Bounds.Width;
        var height = PlotCanvas.Bounds.Height;
        if (values.Count < 2 || width <= 0d || height <= 0d)
        {
            return;
        }

        var maxValue = Math.Max(Max(values), MinimumScaleMs);
        var stepX = width / (values.Count - 1);

        var points = new List<Point>(values.Count);
        for (var i = 0; i < values.Count; i++)
        {
            var x = i * stepX;
            var y = height - (values[i] / maxValue * height);
            points.Add(new Point(x, y));
        }

        PlotCanvas.Children.Add(new Polyline
        {
            Stroke = LineBrush,
            StrokeThickness = 2,
            Points = points
        });
    }

    private List<double> ReadValues()
    {
        var values = new List<double>();
        if (ItemsSource is null)
        {
            return values;
        }

        foreach (var item in ItemsSource)
        {
            if (item is double value)
            {
                values.Add(value);
            }
        }

        return values;
    }

    private static double Max(List<double> values)
    {
        var max = values[0];
        for (var i = 1; i < values.Count; i++)
        {
            if (values[i] > max)
            {
                max = values[i];
            }
        }

        return max;
    }
}
