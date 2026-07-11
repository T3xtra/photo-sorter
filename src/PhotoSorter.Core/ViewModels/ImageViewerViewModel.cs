using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using PhotoSorter.Core.Models;
using PhotoSorter.Core.Services.Abstractions;
using PhotoSorter.Core.Services.Implementations;

namespace PhotoSorter.Core.ViewModels;

/// <summary>
/// ViewModel for the main image display area. Shows the image at the project's current
/// position, exposes next/previous navigation, zoom/pan state, and the sort swipe animation.
/// </summary>
public sealed partial class ImageViewerViewModel : ViewModelBase
{
    /// <summary>10 % .. 800 % manual zoom range.</summary>
    private const double MinZoom = 0.1;

    private const double MaxZoom = 8.0;

    private const double ZoomStep = 1.1;

    /// <summary>Large enough to carry the image well past typical window edges.</summary>
    private const double SwipeDistance = 600;

    private const double SwipeRotationDegrees = 8;

    /// <summary>"Animationsdauer: ca. 150 ms" (SoftwareDesign.md), within the 120-180 ms range from UI-Design.md.</summary>
    private static readonly TimeSpan SwipeAnimationDuration = TimeSpan.FromMilliseconds(150);

    /// <summary>
    /// Beyond the mandatory previous/current/next (SoftwareDesign.md), preload this many more on
    /// each side ("Zusätzliche Bilder nach Bedarf vorladen") - keeps quick repeated navigation
    /// smooth without caching the entire project for very large sets.
    /// </summary>
    private const int PreloadRadius = 2;

    private readonly IProjectService _projectService;
    private readonly IImageCache _imageCache;
    private readonly ILogger<ImageViewerViewModel> _logger;

    private CancellationTokenSource? _loadCancellation;

    [ObservableProperty]
    private byte[]? _currentImageBytes;

    /// <summary>
    /// Set when the current image failed to load (corrupted file, missing file, permission
    /// denied, unsupported format - Roadmap Phase 16); null while loading succeeded or no image
    /// is selected. Shown as an overlay in <c>ImageViewerView</c> so a failure isn't just a
    /// silent blank canvas ("Bildanzeige ist immer zentral", SoftwareDesign.md).
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasImageLoadError))]
    private string? _imageLoadErrorMessage;

    public bool HasImageLoadError => ImageLoadErrorMessage is not null;

    [ObservableProperty]
    private ZoomMode _zoomMode = ZoomMode.FitToWindow;

    /// <summary>Scale applied on top of native (100 %) size while <see cref="ZoomMode"/> is <see cref="ViewModels.ZoomMode.Manual"/>.</summary>
    [ObservableProperty]
    private double _zoomFactor = 1.0;

    [ObservableProperty]
    private double _panOffsetX;

    [ObservableProperty]
    private double _panOffsetY;

    /// <summary>Horizontal swipe-out offset while sorting ("Beim Sortieren: Bild gleitet ... aus dem Fenster").</summary>
    [ObservableProperty]
    private double _swipeOffsetX;

    /// <summary>Slight rotation during the swipe-out (UI-Design.md: "mit leichter Rotation").</summary>
    [ObservableProperty]
    private double _swipeRotationAngle;

    /// <summary>Fades out during the swipe (UI-Design.md: "Optional leichte Transparenz").</summary>
    [ObservableProperty]
    private double _swipeOpacity = 1.0;

    public ImageViewerViewModel(
        IProjectService projectService,
        IImageCache imageCache,
        IUiDispatcher uiDispatcher,
        ISettingsService settingsService,
        ILogger<ImageViewerViewModel> logger)
    {
        _projectService = projectService;
        _imageCache = imageCache;
        _logger = logger;
        AnimationsEnabled = settingsService.Current.AnimationsEnabled;

        // ProjectChanged/CurrentIndexChanged may fire from a background thread (the folder scan
        // runs off the UI thread), so the reaction is marshaled onto the UI thread first - both
        // to safely update CurrentImageBytes (consumed by a UI binding) and because the command
        // CanExecute notifications below must run on the thread the commands were created on.
        _projectService.ProjectChanged += (_, _) => uiDispatcher.Post(OnCurrentImageChanged);
        _projectService.CurrentIndexChanged += (_, _) => uiDispatcher.Post(OnCurrentImageChanged);
        _projectService.DecisionsChanged += (_, _) => uiDispatcher.Post(() => UndoCommand.NotifyCanExecuteChanged());

        // Settings can change live once the Settings window (Phase 15) is open, so this already-
        // constructed singleton needs to pick up "Animationen ein/aus" without a restart.
        settingsService.SettingsChanged += (_, _) => uiDispatcher.Post(() => AnimationsEnabled = settingsService.Current.AnimationsEnabled);

        // Proactively reflect any project already loaded (e.g. restored from the auto-save file
        // at startup, Phase 10) rather than only reacting to events from this point forward.
        OnCurrentImageChanged();
    }

    /// <summary>"Animationen ein/aus" (SoftwareDesign.md). Loaded from settings and kept live via <see cref="ISettingsService.SettingsChanged"/>.</summary>
    [ObservableProperty]
    private bool _animationsEnabled;

    public bool CanGoToNext => _projectService.HasNextImage;

    public bool CanGoToPrevious => _projectService.HasPreviousImage;

    public bool HasCurrentImage => _projectService.CurrentIndex >= 0;

    public bool CanUndo => _projectService.Current.Decisions.Count > 0;

    [RelayCommand(CanExecute = nameof(CanGoToNext))]
    private void NextImage() => _projectService.MoveNext();

    [RelayCommand(CanExecute = nameof(CanGoToPrevious))]
    private void PreviousImage() => _projectService.MovePrevious();

    /// <summary>Sorts the current image to the left target and advances (no file is moved until Phase 11).</summary>
    [RelayCommand(CanExecute = nameof(HasCurrentImage))]
    private Task SortLeft() => AnimateAndRecordAsync(SortAction.Left);

    /// <summary>Sorts the current image to the right target and advances (no file is moved until Phase 11).</summary>
    [RelayCommand(CanExecute = nameof(HasCurrentImage))]
    private Task SortRight() => AnimateAndRecordAsync(SortAction.Right);

    /// <summary>Moves on without deciding ("überspringen"). Not animated - only sort actions swipe (SoftwareDesign.md).</summary>
    [RelayCommand(CanExecute = nameof(CanGoToNext))]
    private void Skip() => _projectService.MoveNext();

    /// <summary>Undoes the most recent decision and jumps back to that image.</summary>
    [RelayCommand(CanExecute = nameof(CanUndo))]
    private void Undo() => _projectService.Undo();

    /// <summary>
    /// Plays the swipe-out animation (if enabled), then records the decision and resets the
    /// transform for the next image. The visual interpolation itself is driven by Avalonia
    /// property transitions in the View once these properties change; this only sequences *when*
    /// they change relative to loading the next image.
    /// </summary>
    private async Task AnimateAndRecordAsync(SortAction action)
    {
        if (AnimationsEnabled)
        {
            var direction = action == SortAction.Left ? -1 : 1;
            SwipeOffsetX = direction * SwipeDistance;
            SwipeRotationAngle = direction * SwipeRotationDegrees;
            SwipeOpacity = 0;

            await Task.Delay(SwipeAnimationDuration);
        }

        _projectService.RecordDecision(action);

        SwipeOffsetX = 0;
        SwipeRotationAngle = 0;
        SwipeOpacity = 1;
    }

    /// <summary>Applies one mouse-wheel zoom step. Positive delta zooms in, negative zooms out.</summary>
    public void ApplyZoomDelta(double wheelDelta)
    {
        var stepFactor = wheelDelta > 0 ? ZoomStep : 1 / ZoomStep;
        var baseline = ZoomMode == ZoomMode.Manual ? ZoomFactor : 1.0;

        ZoomFactor = Math.Clamp(baseline * stepFactor, MinZoom, MaxZoom);
        ZoomMode = ZoomMode.Manual;
    }

    /// <summary>Sets the pan offset while zoomed in. No-op in <see cref="ViewModels.ZoomMode.FitToWindow"/>, where the whole image is already visible.</summary>
    public void SetPan(double offsetX, double offsetY)
    {
        if (ZoomMode != ZoomMode.Manual)
        {
            return;
        }

        PanOffsetX = offsetX;
        PanOffsetY = offsetY;
    }

    /// <summary>Toggles between "fit to window" and "100 %" (SDD/UI-Design: double-click).</summary>
    [RelayCommand]
    private void ToggleZoomMode()
    {
        ZoomMode = ZoomMode == ZoomMode.FitToWindow ? ZoomMode.Manual : ZoomMode.FitToWindow;
        ResetZoomAndPan(keepMode: true);
    }

    /// <summary>Resets back to "Bild einpassen", discarding any manual zoom/pan - unlike <see cref="ToggleZoomMode"/>, this always lands on fit-to-window regardless of the current mode.</summary>
    [RelayCommand]
    private void ResetZoom() => ResetZoomAndPan();

    private void ResetZoomAndPan(bool keepMode = false)
    {
        if (!keepMode)
        {
            ZoomMode = ZoomMode.FitToWindow;
        }

        ZoomFactor = 1.0;
        PanOffsetX = 0;
        PanOffsetY = 0;
    }

    private void OnCurrentImageChanged()
    {
        NextImageCommand.NotifyCanExecuteChanged();
        PreviousImageCommand.NotifyCanExecuteChanged();
        SortLeftCommand.NotifyCanExecuteChanged();
        SortRightCommand.NotifyCanExecuteChanged();
        SkipCommand.NotifyCanExecuteChanged();
        ResetZoomAndPan();

        UpdateCacheWindow();

        // Fire-and-forget: this is a UI-thread-initiated reaction, not something callers await.
        // Failures are caught and logged inside LoadCurrentImageAsync itself.
        _ = LoadCurrentImageAsync();
    }

    /// <summary>Keeps previous/current/next (+/- <see cref="PreloadRadius"/>) cached and everything else evicted.</summary>
    private void UpdateCacheWindow()
    {
        var images = _projectService.Current.Images;
        var index = _projectService.CurrentIndex;

        if (index < 0 || images.Count == 0)
        {
            _imageCache.UpdateWindow([]);
            return;
        }

        var firstIndex = Math.Max(0, index - PreloadRadius);
        var lastIndex = Math.Min(images.Count - 1, index + PreloadRadius);

        var paths = new List<string>(lastIndex - firstIndex + 1);
        for (var i = firstIndex; i <= lastIndex; i++)
        {
            paths.Add(images[i].FullPath);
        }

        _imageCache.UpdateWindow(paths);
    }

    private async Task LoadCurrentImageAsync()
    {
        _loadCancellation?.Cancel();
        var cancellation = new CancellationTokenSource();
        _loadCancellation = cancellation;

        var index = _projectService.CurrentIndex;
        var images = _projectService.Current.Images;

        if (index < 0 || index >= images.Count)
        {
            CurrentImageBytes = null;
            ImageLoadErrorMessage = null;
            return;
        }

        var image = images[index];
        try
        {
            // No ConfigureAwait(false) here: this is ViewModel (UI-layer) code invoked from a
            // UI-thread dispatch, so the continuation below must resume on the UI thread to
            // safely update CurrentImageBytes.
            var bytes = await _imageCache.GetAsync(image.FullPath, cancellation.Token);
            if (!cancellation.IsCancellationRequested)
            {
                CurrentImageBytes = bytes;
                ImageLoadErrorMessage = null;
            }
        }
        catch (OperationCanceledException)
        {
            // Superseded by a newer navigation; nothing to do.
        }
        catch (Exception ex) when (RecoverableImageErrors.IsRecoverable(ex))
        {
            _logger.LogWarning(ex, "Failed to decode image {Path}.", image.FullPath);
            CurrentImageBytes = null;
            ImageLoadErrorMessage = DescribeLoadError(image.FileName, ex);
        }
    }

    /// <summary>User-facing (German) explanation for why the current image can't be shown.</summary>
    private static string DescribeLoadError(string fileName, Exception ex) => ex switch
    {
        System.IO.FileNotFoundException or System.IO.DirectoryNotFoundException =>
            $"Datei nicht gefunden: {fileName}",
        UnauthorizedAccessException =>
            $"Kein Zugriff auf die Datei: {fileName}",
        _ => $"Bild konnte nicht geladen werden (beschädigt oder nicht unterstütztes Format): {fileName}",
    };
}
