using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PhotoSorter.Core.Services.Abstractions;

/// <summary>
/// Keeps decoded (PNG-encoded) images in memory around the current position so navigating
/// between them is close to instant ("Immer im Speicher halten: Vorheriges Bild, Aktuelles
/// Bild, Nächstes Bild", SoftwareDesign.md). Bounded by <see cref="UpdateWindow"/> so memory
/// use stays flat regardless of how many thousand images the project has.
/// </summary>
public interface IImageCache
{
    /// <summary>Returns the decoded bytes for <paramref name="path"/>, decoding and caching them if not already cached.</summary>
    Task<byte[]> GetAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Evicts everything not in <paramref name="pathsToKeep"/> and starts preloading any of
    /// them not yet cached in the background ("Zusätzliche Bilder nach Bedarf vorladen").
    /// </summary>
    void UpdateWindow(IReadOnlyList<string> pathsToKeep);
}
