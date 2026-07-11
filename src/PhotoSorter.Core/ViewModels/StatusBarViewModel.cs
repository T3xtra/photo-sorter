using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PhotoSorter.Core.Services.Abstractions;

namespace PhotoSorter.Core.ViewModels;

/// <summary>
/// ViewModel for the status bar showing the current position and running sort counts.
/// Values default to the "nothing loaded yet" state until a folder is opened (Phase 3)
/// and sorting begins (Phase 8).
/// </summary>
public sealed partial class StatusBarViewModel : ViewModelBase
{
    private readonly IProjectService _projectService;

    /// <summary>
    /// Wired up by <see cref="MainWindowViewModel"/> to <see cref="ImageViewerViewModel.ResetZoomCommand"/>
    /// for the "Zoom zurücksetzen" button next to the zoom display - a plain settable property
    /// (like <see cref="ZoomPercentage"/>) rather than a constructor dependency, so this ViewModel
    /// still doesn't need to reference <see cref="ImageViewerViewModel"/> directly (see
    /// <see cref="MainWindowViewModel"/>'s class doc on bridging sibling state).
    /// </summary>
    public IRelayCommand? ResetZoomCommand { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ImagePositionDisplay))]
    private int _currentIndex;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ImagePositionDisplay))]
    private int _totalCount;

    [ObservableProperty]
    private int _leftCount;

    [ObservableProperty]
    private int _rightCount;

    [ObservableProperty]
    private int _openCount;

    [ObservableProperty]
    private int _zoomPercentage = 100;

    [ObservableProperty]
    private string _currentFileName = string.Empty;

    public StatusBarViewModel(IProjectService projectService, IUiDispatcher uiDispatcher)
    {
        _projectService = projectService;

        // ProjectChanged/CurrentIndexChanged may fire from a background thread (the folder scan
        // runs off the UI thread), so the update is marshaled onto the UI thread before touching
        // bindable state.
        projectService.ProjectChanged += (_, _) => uiDispatcher.Post(ApplyProject);
        projectService.CurrentIndexChanged += (_, _) => uiDispatcher.Post(ApplyProject);
        projectService.DecisionsChanged += (_, _) => uiDispatcher.Post(ApplyProject);

        ApplyProject();
    }

    /// <summary>Human-readable "current / total" position, e.g. "Bild 127 / 984".</summary>
    public string ImagePositionDisplay => TotalCount == 0
        ? "Kein Bild geladen"
        : $"Bild {CurrentIndex} / {TotalCount}";

    private void ApplyProject()
    {
        var project = _projectService.Current;

        TotalCount = project.Images.Count;
        LeftCount = project.LeftCount;
        RightCount = project.RightCount;
        OpenCount = project.OpenCount;
        CurrentIndex = _projectService.CurrentIndex + 1;
        CurrentFileName = _projectService.CurrentIndex >= 0 && _projectService.CurrentIndex < project.Images.Count
            ? project.Images[_projectService.CurrentIndex].FileName
            : string.Empty;
    }
}
