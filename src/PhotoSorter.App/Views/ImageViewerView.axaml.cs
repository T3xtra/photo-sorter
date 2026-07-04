using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using PhotoSorter.Core.ViewModels;

namespace PhotoSorter.App.Views;

public partial class ImageViewerView : UserControl
{
    private Point? _panStartPointerPosition;
    private double _panStartOffsetX;
    private double _panStartOffsetY;

    public ImageViewerView()
    {
        InitializeComponent();
        PointerWheelChanged += OnPointerWheelChanged;
        PointerPressed += OnPointerPressed;
        PointerMoved += OnPointerMoved;
        PointerReleased += OnPointerReleased;
        DoubleTapped += OnDoubleTapped;
    }

    private ImageViewerViewModel? ViewModel => DataContext as ImageViewerViewModel;

    private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        ViewModel?.ApplyZoomDelta(e.Delta.Y);
        e.Handled = true;
    }

    /// <summary>
    /// Middle mouse button pans (see docs/architecture-decisions.md, Punkt 1: the left mouse
    /// button is deliberately kept free for the optional drag-to-sort gesture).
    /// </summary>
    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsMiddleButtonPressed)
        {
            return;
        }

        _panStartPointerPosition = e.GetPosition(this);
        _panStartOffsetX = ViewModel?.PanOffsetX ?? 0;
        _panStartOffsetY = ViewModel?.PanOffsetY ?? 0;
        e.Pointer.Capture(this);
        e.Handled = true;
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_panStartPointerPosition is not { } start || ViewModel is not { } viewModel)
        {
            return;
        }

        var current = e.GetPosition(this);
        viewModel.SetPan(_panStartOffsetX + (current.X - start.X), _panStartOffsetY + (current.Y - start.Y));
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_panStartPointerPosition is null)
        {
            return;
        }

        _panStartPointerPosition = null;
        e.Pointer.Capture(null);
    }

    private void OnDoubleTapped(object? sender, TappedEventArgs e) =>
        ViewModel?.ToggleZoomModeCommand.Execute(null);
}
