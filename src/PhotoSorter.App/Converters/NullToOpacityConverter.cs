using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace PhotoSorter.App.Converters;

/// <summary>
/// Drives the thumbnail fade-in ("sanfte Übergänge", Roadmap Phase 12): 0 while a thumbnail's
/// bytes haven't loaded yet, 1 once they have. Combined with an Opacity transition in XAML, each
/// thumbnail fades in as it finishes generating instead of popping in abruptly.
/// </summary>
public sealed class NullToOpacityConverter : IValueConverter
{
    public static readonly NullToOpacityConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is null ? 0d : 1d;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
