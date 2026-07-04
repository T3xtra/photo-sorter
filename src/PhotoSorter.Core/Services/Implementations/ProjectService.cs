using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PhotoSorter.Core.Models;
using PhotoSorter.Core.Models.Persistence;
using PhotoSorter.Core.Services.Abstractions;

namespace PhotoSorter.Core.Services.Implementations;

/// <inheritdoc cref="IProjectService"/>
public sealed class ProjectService : IProjectService
{
    private readonly IFolderScannerService _folderScanner;
    private readonly IProjectFileSerializer _fileSerializer;
    private readonly string _autoSaveFilePath;
    private readonly ILogger<ProjectService> _logger;

    public ProjectService(
        IFolderScannerService folderScanner,
        IProjectFileSerializer fileSerializer,
        IAppPathProvider pathProvider,
        ILogger<ProjectService> logger)
    {
        _folderScanner = folderScanner;
        _fileSerializer = fileSerializer;
        _autoSaveFilePath = pathProvider.AutoSaveFilePath;
        _logger = logger;

        // "Während des Sortierens wird automatisch eine Projektdatei gespeichert" (SoftwareDesign.md):
        // save on every project load and every decision/undo. Plain navigation (CurrentIndexChanged
        // alone) does not trigger a save - that would write to disk on every arrow key press.
        ProjectChanged += (_, _) => _ = AutoSaveAsync();
        DecisionsChanged += (_, _) => _ = AutoSaveAsync();
    }

    public Project Current { get; } = new();

    public int CurrentIndex { get; private set; } = -1;

    public event EventHandler? ProjectChanged;

    public event EventHandler? CurrentIndexChanged;

    public event EventHandler? DecisionsChanged;

    public event EventHandler? TargetsChanged;

    public async Task LoadFromFoldersAsync(IReadOnlyList<string> sourceFolders, CancellationToken cancellationToken = default)
    {
        var images = await _folderScanner.ScanAsync(sourceFolders, cancellationToken).ConfigureAwait(false);

        Current.Reset(sourceFolders, images);
        CurrentIndex = FindNextOpenIndex(0);

        _logger.LogInformation("Project loaded: {Count} image(s) from {FolderCount} folder(s).", images.Count, sourceFolders.Count);

        ProjectChanged?.Invoke(this, EventArgs.Empty);
    }

    public bool HasNextImage => FindNextOpenIndex(CurrentIndex + 1) >= 0;

    public bool HasPreviousImage => FindPreviousOpenIndex(CurrentIndex - 1) >= 0;

    public bool MoveNext() => MoveTo(FindNextOpenIndex(CurrentIndex + 1));

    public bool MovePrevious() => MoveTo(FindPreviousOpenIndex(CurrentIndex - 1));

    public bool MoveTo(int index)
    {
        if (index < 0 || index >= Current.Images.Count || index == CurrentIndex)
        {
            return false;
        }

        CurrentIndex = index;
        CurrentIndexChanged?.Invoke(this, EventArgs.Empty);
        return true;
    }

    public void RecordDecision(SortAction action)
    {
        if (CurrentIndex < 0 || CurrentIndex >= Current.Images.Count)
        {
            return;
        }

        var image = Current.Images[CurrentIndex];
        Current.AddDecision(new SortDecision(image, action));
        DecisionsChanged?.Invoke(this, EventArgs.Empty);

        var nextIndex = FindNextOpenIndex(CurrentIndex + 1);
        CurrentIndex = nextIndex;
        CurrentIndexChanged?.Invoke(this, EventArgs.Empty);
    }

    public bool Undo()
    {
        var undone = Current.RemoveLastDecision();
        if (undone is null)
        {
            return false;
        }

        DecisionsChanged?.Invoke(this, EventArgs.Empty);

        var restoredIndex = IndexOf(undone.Image);
        if (restoredIndex >= 0 && restoredIndex != CurrentIndex)
        {
            CurrentIndex = restoredIndex;
            CurrentIndexChanged?.Invoke(this, EventArgs.Empty);
        }

        return true;
    }

    public void SetLeftTarget(TargetFolder target)
    {
        Current.LeftTarget = target;
        TargetsChanged?.Invoke(this, EventArgs.Empty);
    }

    public void SetRightTarget(TargetFolder target)
    {
        Current.RightTarget = target;
        TargetsChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task SaveAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var dto = new ProjectFileDto
        {
            SourceFolders = Current.SourceFolders.ToList(),
            ImagePaths = Current.Images.Select(i => i.FullPath).ToList(),
            LeftTarget = ToDto(Current.LeftTarget),
            RightTarget = ToDto(Current.RightTarget),
            Decisions = Current.Decisions.Select(d => new DecisionDto { ImagePath = d.Image.FullPath, Action = d.Action }).ToList(),
            CurrentIndex = CurrentIndex,
        };

        await _fileSerializer.SaveAsync(dto, filePath, cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> OpenAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var dto = await _fileSerializer.LoadAsync(filePath, cancellationToken).ConfigureAwait(false);
        if (dto is null)
        {
            return false;
        }

        var images = new List<ImageFile>();
        foreach (var path in dto.ImagePaths)
        {
            if (!File.Exists(path))
            {
                _logger.LogWarning("Image {Path} referenced by project file no longer exists, skipping.", path);
                continue;
            }

            var info = new FileInfo(path);
            images.Add(new ImageFile { FullPath = path, FileName = info.Name, SizeInBytes = info.Length });
        }

        Current.Reset(dto.SourceFolders, images);
        Current.LeftTarget = FromDto(dto.LeftTarget);
        Current.RightTarget = FromDto(dto.RightTarget);

        var imagesByPath = images.ToDictionary(i => i.FullPath);
        foreach (var decisionDto in dto.Decisions)
        {
            if (imagesByPath.TryGetValue(decisionDto.ImagePath, out var image))
            {
                Current.AddDecision(new SortDecision(image, decisionDto.Action));
            }
        }

        CurrentIndex = dto.CurrentIndex >= 0 && dto.CurrentIndex < images.Count && !Current.IsDecided(images[dto.CurrentIndex])
            ? dto.CurrentIndex
            : FindNextOpenIndex(0);

        _logger.LogInformation("Project opened from {Path}: {Count} image(s), {Decisions} decision(s).", filePath, images.Count, Current.Decisions.Count);

        ProjectChanged?.Invoke(this, EventArgs.Empty);
        TargetsChanged?.Invoke(this, EventArgs.Empty);
        return true;
    }

    public void RemoveAppliedImages(IReadOnlyList<ImageFile> images)
    {
        if (images.Count == 0)
        {
            return;
        }

        var currentImage = CurrentIndex >= 0 && CurrentIndex < Current.Images.Count ? Current.Images[CurrentIndex] : null;

        Current.RemoveImages(images);

        CurrentIndex = currentImage is not null ? IndexOf(currentImage) : -1;
        if (CurrentIndex < 0)
        {
            CurrentIndex = FindNextOpenIndex(0);
        }

        _logger.LogInformation("Removed {Count} applied image(s) from the project.", images.Count);

        ProjectChanged?.Invoke(this, EventArgs.Empty);
    }

    private async Task AutoSaveAsync()
    {
        try
        {
            await SaveAsync(_autoSaveFilePath).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogWarning(ex, "Auto-save failed.");
        }
    }

    private static TargetFolderDto? ToDto(TargetFolder? target) =>
        target is null ? null : new TargetFolderDto { IsTrash = target.IsTrash, Path = target.Path };

    private static TargetFolder? FromDto(TargetFolderDto? dto) =>
        dto is null ? null : new TargetFolder(dto.IsTrash, dto.Path);

    private int IndexOf(ImageFile image)
    {
        var images = Current.Images;
        for (var i = 0; i < images.Count; i++)
        {
            if (ReferenceEquals(images[i], image))
            {
                return i;
            }
        }

        return -1;
    }

    private int FindNextOpenIndex(int fromIndex)
    {
        var images = Current.Images;
        for (var i = Math.Max(fromIndex, 0); i < images.Count; i++)
        {
            if (!Current.IsDecided(images[i]))
            {
                return i;
            }
        }

        return -1;
    }

    private int FindPreviousOpenIndex(int fromIndex)
    {
        var images = Current.Images;
        for (var i = Math.Min(fromIndex, images.Count - 1); i >= 0; i--)
        {
            if (!Current.IsDecided(images[i]))
            {
                return i;
            }
        }

        return -1;
    }
}
