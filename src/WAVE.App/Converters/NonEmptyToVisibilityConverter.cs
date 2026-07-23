using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace WAVE.App.Converters;

/// <summary>
/// Shows an element only when the bound string has content; collapses it when the string
/// is null or whitespace. Reusable across any "show this label only when set" case.
/// </summary>
public sealed class NonEmptyToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        string.IsNullOrWhiteSpace(value as string) ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
