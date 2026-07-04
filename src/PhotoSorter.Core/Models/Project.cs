using System;
using System.Collections.Generic;
using System.Linq;

namespace PhotoSorter.Core.Models;

/// <summary>
/// The current sorting session: source and target folders, the images loaded from the source
/// folders (in their original, stable order - needed for Undo and for persisting "Reihenfolge"
/// in Phase 10), and the sort decisions made so far. Persisted as a <c>.photosort</c> project
/// file in Phase 10.
///
/// Deliberately a plain data holder, not an <see cref="System.Collections.ObjectModel.ObservableCollection{T}"/>:
/// Core knows nothing about UI threads, so it must not expose UI-bindable collections that
/// could be mutated off the UI thread. ViewModels project this into their own UI-bound
/// collections after reacting to <see cref="Services.Abstractions.IProjectService.ProjectChanged"/>.
/// </summary>
public sealed class Project
{
    private readonly List<SortDecision> _decisions = [];
    private readonly HashSet<ImageFile> _decidedImages = [];

    public IReadOnlyList<string> SourceFolders { get; private set; } = Array.Empty<string>();

    public IReadOnlyList<ImageFile> Images { get; private set; } = Array.Empty<ImageFile>();

    public TargetFolder? LeftTarget { get; internal set; }

    public TargetFolder? RightTarget { get; internal set; }

    /// <summary>Decisions in the order they were made (append-only during sorting, popped by Undo in Phase 9).</summary>
    public IReadOnlyList<SortDecision> Decisions => _decisions;

    public int LeftCount => _decisions.Count(d => d.Action == SortAction.Left);

    public int RightCount => _decisions.Count(d => d.Action == SortAction.Right);

    /// <summary>Images not yet sorted.</summary>
    public int OpenCount => Images.Count - _decisions.Count;

    public bool IsDecided(ImageFile image) => _decidedImages.Contains(image);

    internal void Reset(IReadOnlyList<string> sourceFolders, IReadOnlyList<ImageFile> images)
    {
        SourceFolders = sourceFolders;
        Images = images;
        _decisions.Clear();
        _decidedImages.Clear();
    }

    internal void AddDecision(SortDecision decision)
    {
        _decisions.Add(decision);
        _decidedImages.Add(decision.Image);
    }

    /// <summary>Removes and returns the most recent decision, or null if there is none (Phase 9: Undo).</summary>
    internal SortDecision? RemoveLastDecision()
    {
        if (_decisions.Count == 0)
        {
            return null;
        }

        var last = _decisions[^1];
        _decisions.RemoveAt(_decisions.Count - 1);
        _decidedImages.Remove(last.Image);
        return last;
    }

    /// <summary>
    /// Permanently removes images from the session after they have been successfully moved to
    /// their target (Phase 11). Unlike sorting/undo, this really does shrink <see cref="Images"/>:
    /// once a file has been moved on disk, there is nothing left to undo back to.
    /// </summary>
    internal void RemoveImages(IReadOnlyCollection<ImageFile> images)
    {
        if (images.Count == 0)
        {
            return;
        }

        var toRemove = new HashSet<ImageFile>(images);
        Images = Images.Where(image => !toRemove.Contains(image)).ToList();
        _decisions.RemoveAll(d => toRemove.Contains(d.Image));

        foreach (var image in toRemove)
        {
            _decidedImages.Remove(image);
        }
    }
}
