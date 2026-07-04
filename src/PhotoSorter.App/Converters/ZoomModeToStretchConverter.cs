using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using PhotoSorter.Core.ViewModels;

namespace PhotoSorter.App.Converters;

/// <summary>
/// "Fit to window" scales the image down/up to fit (<see cref="Stretch.Uniform"/>). "Manual"
/// (100 % and beyond) shows native pixels (<see cref="Stretch.None"/>), with further zoom applied
/// via <c>ImageViewerViewModel.ZoomFactor</c> as an additional render transform.
/// </summary>
public sealed class ZoomModeToStretchConverter : IValueConverter
{
    public static readonly ZoomModeToStretchConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is ZoomMode.Manual ? Stretch.None : Stretch.Uniform;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
