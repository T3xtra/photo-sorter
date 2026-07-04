using System;
using System.Globalization;
using System.IO;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;

namespace PhotoSorter.App.Converters;

/// <summary>
/// Converts the PNG bytes produced by <c>IImageDecoder</c> (Core, UI-framework-free) into an
/// Avalonia <see cref="Bitmap"/> for display. This conversion must happen in the App project:
/// constructing a Bitmap requires Avalonia's platform render interface, which only exists once
/// the application has started.
/// </summary>
public sealed class PngBytesToBitmapConverter : IValueConverter
{
    public static readonly PngBytesToBitmapConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not byte[] { Length: > 0 } bytes)
        {
            return null;
        }

        using var stream = new MemoryStream(bytes);
        return new Bitmap(stream);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
