using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using WAVE.Domain.Testing;

namespace WAVE.App.Converters;

/// <summary>Converts the operation state into the corresponding visual-feedback brush.</summary>
public sealed class StateToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var resourceKey = value is TestOperationState state
            ? state switch
            {
                TestOperationState.Connecting => "BrushConnecting",
                TestOperationState.TestRunning => "BrushRunning",
                TestOperationState.Failed => "BrushFailed",
                _ => "BrushIdle"
            }
            : "BrushIdle";

        return System.Windows.Application.Current?.TryFindResource(resourceKey) as Brush ?? Brushes.Gray;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
