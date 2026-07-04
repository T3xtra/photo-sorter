using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace PhotoSorter.App.Converters;

/// <summary>
/// Highlights the current thumbnail with the "Markierung: Orange" accent from UI-Design.md
/// ("Dark Mode" section), transparent otherwise.
/// </summary>
public sealed class CurrentThumbnailBorderBrushConverter : IValueConverter
{
    public static readonly CurrentThumbnailBorderBrushConverter Instance = new();

    private static readonly IBrush Highlight = new SolidColorBrush(Colors.Orange);

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? Highlight : Brushes.Transparent;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
