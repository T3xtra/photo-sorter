using System;

namespace PhotoSorter.Core.ViewModels;

/// <summary>
/// ViewModel for the application's main window. Composes the toolbar, thumbnail bar,
/// image viewer and status bar ViewModels that make up the main layout, and bridges
/// state between siblings that shouldn't reference each other directly (e.g. the status
/// bar's zoom display reflecting the image viewer's zoom state).
/// </summary>
public sealed class MainWindowViewModel : ViewModelBase
{
    public MainWindowViewModel(
        ToolbarViewModel toolbarViewModel,
        ThumbnailBarViewModel thumbnailBarViewModel,
        ImageViewerViewModel imageViewerViewModel,
        StatusBarViewModel statusBarViewModel)
    {
        ToolbarViewModel = toolbarViewModel;
        ThumbnailBarViewModel = thumbnailBarViewModel;
        ImageViewerViewModel = imageViewerViewModel;
        StatusBarViewModel = statusBarViewModel;

        imageViewerViewModel.PropertyChanged += OnImageViewerPropertyChanged;
        UpdateZoomDisplay();
        StatusBarViewModel.ResetZoomCommand = imageViewerViewModel.ResetZoomCommand;
    }

    public ToolbarViewModel ToolbarViewModel { get; }

    public ThumbnailBarViewModel ThumbnailBarViewModel { get; }

    public ImageViewerViewModel ImageViewerViewModel { get; }

    public StatusBarViewModel StatusBarViewModel { get; }

    private void OnImageViewerPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ImageViewerViewModel.ZoomFactor) or nameof(ImageViewerViewModel.ZoomMode))
        {
            UpdateZoomDisplay();
        }
    }

    private void UpdateZoomDisplay()
    {
        StatusBarViewModel.ZoomPercentage = (int)Math.Round(ImageViewerViewModel.ZoomFactor * 100);
    }
}
