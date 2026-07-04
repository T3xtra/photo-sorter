using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using PhotoSorter.Core.Services.Abstractions;
using PhotoSorter.Core.Services.Implementations;

namespace PhotoSorter.Core.ViewModels;

/// <summary>
/// ViewModel for the horizontally scrolling thumbnail strip. Thumbnails are generated in the
/// background, with bounded concurrency so loading thousands of images doesn't spike memory/CPU.
/// </summary>
public sealed partial class ThumbnailBarViewModel : ViewModelBase
{
    private readonly IProjectService _projectService;
    private readonly IThumbnailGenerator _thumbnailGenerator;
    private readonly IUiDispatcher _uiDispatcher;
    private readonly ILogger<ThumbnailBarViewModel> _logger;

    private CancellationTokenSource? _generationCancellation;

    [ObservableProperty]
    private ThumbnailItemViewModel? _selectedThumbnail;

    public ThumbnailBarViewModel(
        IProjectService projectService,
        IThumbnailGenerator thumbnailGenerator,
        IUiDispatcher uiDispatcher,
        ILogger<ThumbnailBarViewModel> logger)
    {
        _projectService = projectService;
        _thumbnailGenerator = thumbnailGenerator;
        _uiDispatcher = uiDispatcher;
        _logger = logger;

        _projectService.ProjectChanged += (_, _) => uiDispatcher.Post(OnProjectChanged);
        _projectService.CurrentIndexChanged += (_, _) => uiDispatcher.Post(UpdateCurrentHighlight);
        _projectService.DecisionsChanged += (_, _) => uiDispatcher.Post(RemoveDecidedThumbnails);

        // Proactively reflect any project already loaded (e.g. restored from the auto-save file
        // at startup, Phase 10) rather than only reacting to events from this point forward.
        OnProjectChanged();
    }

    public ObservableCollection<ThumbnailItemViewModel> Thumbnails { get; } = new();

    private void OnProjectChanged()
    {
        _generationCancellation?.Cancel();
        var cancellation = new CancellationTokenSource();
        _generationCancellation = cancellation;

        Thumbnails.Clear();
        // Skip already-decided images: relevant when a project is restored mid-sort (Phase 10),
        // where Project.Images may include images decided in a previous session.
        foreach (var image in _projectService.Current.Images.Where(image => !_projectService.Current.IsDecided(image)))
        {
            Thumbnails.Add(new ThumbnailItemViewModel(image, _projectService));
        }

        UpdateCurrentHighlight();

        _ = GenerateThumbnailsAsync(Thumbnails.ToList(), cancellation.Token);
    }

    /// <summary>
    /// Matches by image reference rather than list position: once <see cref="RemoveDecidedThumbnails"/>
    /// has removed sorted items, <c>Thumbnails[i]</c> no longer corresponds to <c>Project.Images[i]</c>.
    /// </summary>
    private void UpdateCurrentHighlight()
    {
        var currentIndex = _projectService.CurrentIndex;
        var images = _projectService.Current.Images;
        var currentImage = currentIndex >= 0 && currentIndex < images.Count ? images[currentIndex] : null;

        ThumbnailItemViewModel? selected = null;
        foreach (var thumbnail in Thumbnails)
        {
            var isCurrent = currentImage is not null && ReferenceEquals(thumbnail.ImageFile, currentImage);
            thumbnail.IsCurrent = isCurrent;
            if (isCurrent)
            {
                selected = thumbnail;
            }
        }

        SelectedThumbnail = selected;
    }

    /// <summary>"bereits sortierte Bilder verschwinden" (UI-Design.md, Thumbnail-Leiste).</summary>
    private void RemoveDecidedThumbnails()
    {
        for (var i = Thumbnails.Count - 1; i >= 0; i--)
        {
            if (_projectService.Current.IsDecided(Thumbnails[i].ImageFile))
            {
                Thumbnails.RemoveAt(i);
            }
        }

        UpdateCurrentHighlight();
    }

    private async Task GenerateThumbnailsAsync(IReadOnlyList<ThumbnailItemViewModel> items, CancellationToken cancellationToken)
    {
        var maxConcurrency = Math.Max(2, Environment.ProcessorCount / 2);

        await Parallel.ForEachAsync(
            items,
            new ParallelOptions { MaxDegreeOfParallelism = maxConcurrency, CancellationToken = cancellationToken },
            async (item, itemCancellationToken) =>
            {
                try
                {
                    var bytes = await _thumbnailGenerator
                        .GenerateAsync(item.ImageFile.FullPath, itemCancellationToken)
                        .ConfigureAwait(false);

                    _uiDispatcher.Post(() => item.ThumbnailBytes = bytes);
                }
                catch (OperationCanceledException)
                {
                    // Superseded by a newer project load; nothing to do.
                }
                catch (Exception ex) when (RecoverableImageErrors.IsRecoverable(ex))
                {
                    _logger.LogWarning(ex, "Failed to generate thumbnail for {Path}.", item.ImageFile.FullPath);
                    _uiDispatcher.Post(() => item.HasError = true);
                }
            }).ConfigureAwait(false);
    }
}
