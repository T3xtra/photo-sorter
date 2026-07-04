using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PhotoSorter.Core.Services.Abstractions;

/// <summary>
/// Opens a native folder picker. The implementation is platform/UI-specific (it needs a
/// window to attach the dialog to), so it lives in the App project - ViewModels only see
/// this Core-declared abstraction, keeping the dialog framework detail out of MVVM logic.
/// </summary>
public interface IFolderPickerService
{
    /// <summary>
    /// Opens a folder picker. Returns an empty list if the user cancels.
    /// </summary>
    /// <param name="suggestedStartLocation">
    /// "Ordner merken" (Roadmap Phase 15): a previously used folder to open the dialog at, or
    /// null for the platform default. Ignored if the path no longer exists.
    /// </param>
    Task<IReadOnlyList<string>> PickFoldersAsync(string title, string? suggestedStartLocation = null, CancellationToken cancellationToken = default);

    /// <summary>Opens a single-folder picker (e.g. choosing a sort target). Returns null if the user cancels.</summary>
    /// <param name="suggestedStartLocation">See <see cref="PickFoldersAsync"/>.</param>
    Task<string?> PickFolderAsync(string title, string? suggestedStartLocation = null, CancellationToken cancellationToken = default);
}
