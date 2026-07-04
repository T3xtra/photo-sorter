using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PhotoSorter.Core.Models;

namespace PhotoSorter.Core.Services.Abstractions;

/// <summary>
/// Owns the current sorting session (<see cref="Project"/>), the position within it, and the
/// sort decisions made so far. Grows in later phases to also persist and restore projects
/// (Phase 10) and execute the actual file moves (Phase 11).
/// </summary>
public interface IProjectService
{
    /// <summary>The current project. Has no images until <see cref="LoadFromFoldersAsync"/> completes.</summary>
    Project Current { get; }

    /// <summary>
    /// Index of the currently displayed (undecided) image within <see cref="Project.Images"/>,
    /// or -1 if there is none (nothing loaded, or everything has been sorted).
    /// </summary>
    int CurrentIndex { get; }

    /// <summary>
    /// Raised after <see cref="Current"/> changes. May be raised from a background thread;
    /// subscribers must marshal to the UI thread themselves (see <see cref="IUiDispatcher"/>).
    /// </summary>
    event EventHandler? ProjectChanged;

    /// <summary>Raised after <see cref="CurrentIndex"/> changes. Same threading caveat as <see cref="ProjectChanged"/>.</summary>
    event EventHandler? CurrentIndexChanged;

    /// <summary>Raised after a decision is recorded or undone. Same threading caveat as <see cref="ProjectChanged"/>.</summary>
    event EventHandler? DecisionsChanged;

    /// <summary>Scans the given source folders and replaces <see cref="Current"/> with the result.</summary>
    Task LoadFromFoldersAsync(IReadOnlyList<string> sourceFolders, CancellationToken cancellationToken = default);

    /// <summary>Whether <see cref="MoveNext"/> would succeed.</summary>
    bool HasNextImage { get; }

    /// <summary>Whether <see cref="MovePrevious"/> would succeed.</summary>
    bool HasPreviousImage { get; }

    /// <summary>Moves to the next undecided image. Returns false if there is none.</summary>
    bool MoveNext();

    /// <summary>Moves to the previous undecided image. Returns false if there is none.</summary>
    bool MovePrevious();

    /// <summary>Moves directly to the given index (e.g. a thumbnail click). Returns false if out of range.</summary>
    bool MoveTo(int index);

    /// <summary>
    /// Records <paramref name="action"/> for the current image and advances to the next
    /// undecided one. No-op if there is no current image. No file is moved (Phase 11).
    /// </summary>
    void RecordDecision(SortAction action);

    /// <summary>
    /// Undoes the most recent decision (any number of times, back to the start of the session)
    /// and jumps back to that image so it can be re-decided. Returns false if there is nothing
    /// to undo. No file has been moved yet at this stage (Phase 11), so this is a pure in-memory
    /// operation.
    /// </summary>
    bool Undo();

    /// <summary>Raised after <see cref="Project.LeftTarget"/> or <see cref="Project.RightTarget"/> changes.</summary>
    event EventHandler? TargetsChanged;

    void SetLeftTarget(TargetFolder target);

    void SetRightTarget(TargetFolder target);

    /// <summary>
    /// Saves the current project to <paramref name="filePath"/> as a <c>.photosort</c> file
    /// (SourceFolders, Reihenfolge, Zielordner, Entscheidungen, aktuelle Position).
    /// </summary>
    Task SaveAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Replaces <see cref="Current"/> with the project loaded from <paramref name="filePath"/>.
    /// Images that no longer exist on disk are skipped with a warning. Returns false if the
    /// file doesn't exist or isn't a valid project file.
    /// </summary>
    Task<bool> OpenAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes images that were successfully moved to their target (Phase 11: after
    /// <c>IFileMoveService.ApplyAsync</c>). Images that failed to move are left untouched so the
    /// user can retry. Raises <see cref="ProjectChanged"/>.
    /// </summary>
    void RemoveAppliedImages(IReadOnlyList<ImageFile> images);
}
