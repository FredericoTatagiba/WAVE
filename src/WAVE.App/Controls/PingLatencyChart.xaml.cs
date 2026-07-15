using System.Collections;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace WAVE.App.Controls;

/// <summary>
/// Live latency chart component. Draws a polyline from a collection of values (ms)
/// and redraws when the collection or the size changes.
/// No external charting-library dependencies.
/// </summary>
public partial class PingLatencyChart : UserControl
{
    private const double MinimumScaleMs = 50d;

    public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
        nameof(ItemsSource),
        typeof(IEnumerable),
        typeof(PingLatencyChart),
        new PropertyMetadata(null, OnItemsSourceChanged));

    private INotifyCollectionChanged? _observable;

    public PingLatencyChart()
    {
        InitializeComponent();
        SizeChanged += (_, _) => Redraw();
    }

    public IEnumerable? ItemsSource
    {
        get => (IEnumerable?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var chart = (PingLatencyChart)d;

        if (chart._observable is not null)
        {
            chart._observable.CollectionChanged -= chart.OnCollectionChanged;
        }

        chart._observable = e.NewValue as INotifyCollectionChanged;

        if (chart._observable is not null)
        {
            chart._observable.CollectionChanged += chart.OnCollectionChanged;
        }

        chart.Redraw();
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => Redraw();

    private void Redraw()
    {
        PlotCanvas.Children.Clear();

        var values = ReadValues();
        EmptyLabel.Visibility = values.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

        var width = PlotCanvas.ActualWidth;
        var height = PlotCanvas.ActualHeight;
        if (values.Count < 2 || width <= 0d || height <= 0d)
        {
            return;
        }

        var maxValue = Math.Max(Max(values), MinimumScaleMs);
        var stepX = width / (values.Count - 1);

        var points = new PointCollection(values.Count);
        for (var i = 0; i < values.Count; i++)
        {
            var x = i * stepX;
            var y = height - (values[i] / maxValue * height);
            points.Add(new Point(x, y));
        }

        PlotCanvas.Children.Add(new Polyline
        {
            Stroke = new SolidColorBrush(Color.FromRgb(0x4C, 0xC2, 0xFF)),
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
