using System;
using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Threading;
using PhotoSorter.Core.ViewModels;

namespace PhotoSorter.App.Views;

public partial class ThumbnailBarView : UserControl
{
    private ThumbnailBarViewModel? _viewModel;

    public ThumbnailBarView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }

        _viewModel = DataContext as ThumbnailBarViewModel;

        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        }
    }

    /// <summary>
    /// Keeps the current thumbnail visible ("automatisch mitscrollend" / "automatische
    /// Zentrierung" per SoftwareDesign.md / UI-Design.md). This is view-specific scroll
    /// behavior, not business logic, so it belongs in code-behind rather than the ViewModel.
    /// </summary>
    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(ThumbnailBarViewModel.SelectedThumbnail))
        {
            return;
        }

        var selected = _viewModel?.SelectedThumbnail;
        if (selected is null)
        {
            return;
        }

        // Deferred: the ItemsControl needs a layout pass to realize the container for a
        // just-added/just-selected item before it can be scrolled into view.
        Dispatcher.UIThread.Post(
            () => ThumbnailItemsControl.ContainerFromItem(selected)?.BringIntoView(),
            DispatcherPriority.Background);
    }
}
