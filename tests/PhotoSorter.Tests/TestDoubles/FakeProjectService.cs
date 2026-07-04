using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PhotoSorter.Core.Models;
using PhotoSorter.Core.Services.Abstractions;

namespace PhotoSorter.Tests.TestDoubles;

/// <summary>
/// A controllable <see cref="IProjectService"/> for tests: <see cref="LoadFromFoldersAsync"/>
/// just records the requested folders instead of touching the real file system, and
/// <see cref="RaiseProjectChanged"/> lets a test simulate the event firing from any thread.
/// Mirrors the "skip decided images" navigation semantics of the real <c>ProjectService</c>.
/// </summary>
public sealed class FakeProjectService : IProjectService
{
    public Project Current { get; } = new();

    public int CurrentIndex { get; private set; } = -1;

    public event EventHandler? ProjectChanged;

    public event EventHandler? CurrentIndexChanged;

    public event EventHandler? DecisionsChanged;

    public event EventHandler? TargetsChanged;

    public IReadOnlyList<string>? LastRequestedFolders { get; private set; }

    public string? LastSavedPath { get; private set; }

    public string? LastOpenedPath { get; private set; }

    public bool OpenResult { get; set; } = true;

    public int LoadCallCount { get; private set; }

    public bool HasNextImage => FindNextOpenIndex(CurrentIndex + 1) >= 0;

    public bool HasPreviousImage => FindPreviousOpenIndex(CurrentIndex - 1) >= 0;

    public Task LoadFromFoldersAsync(IReadOnlyList<string> sourceFolders, CancellationToken cancellationToken = default)
    {
        LoadCallCount++;
        LastRequestedFolders = sourceFolders;
        return Task.CompletedTask;
    }

    public bool MoveNext() => MoveTo(FindNextOpenIndex(CurrentIndex + 1));

    public bool MovePrevious() => MoveTo(FindPreviousOpenIndex(CurrentIndex - 1));

    public void SetImages(IReadOnlyList<ImageFile> images)
    {
        Current.Reset([], images);
        CurrentIndex = images.Count > 0 ? 0 : -1;
    }

    public void RaiseProjectChanged() => ProjectChanged?.Invoke(this, EventArgs.Empty);

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

        Current.AddDecision(new SortDecision(Current.Images[CurrentIndex], action));
        DecisionsChanged?.Invoke(this, EventArgs.Empty);

        CurrentIndex = FindNextOpenIndex(CurrentIndex + 1);
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

    public Task SaveAsync(string filePath, CancellationToken cancellationToken = default)
    {
        LastSavedPath = filePath;
        return Task.CompletedTask;
    }

    public Task<bool> OpenAsync(string filePath, CancellationToken cancellationToken = default)
    {
        LastOpenedPath = filePath;
        return Task.FromResult(OpenResult);
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

        ProjectChanged?.Invoke(this, EventArgs.Empty);
    }

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
