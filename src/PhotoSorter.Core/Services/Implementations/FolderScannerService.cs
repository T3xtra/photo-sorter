using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PhotoSorter.Core.Configuration;
using PhotoSorter.Core.Models;
using PhotoSorter.Core.Services.Abstractions;

namespace PhotoSorter.Core.Services.Implementations;

/// <inheritdoc cref="IFolderScannerService"/>
public sealed class FolderScannerService : IFolderScannerService
{
    private readonly ILogger<FolderScannerService> _logger;

    public FolderScannerService(ILogger<FolderScannerService> logger)
    {
        _logger = logger;
    }

    public Task<IReadOnlyList<ImageFile>> ScanAsync(IReadOnlyList<string> sourceFolders, CancellationToken cancellationToken = default)
    {
        // Directory enumeration is synchronous I/O; running it on a pool thread keeps the
        // caller (a UI command) from blocking while thousands of files are scanned.
        return Task.Run(() => Scan(sourceFolders, cancellationToken), cancellationToken);
    }

    private IReadOnlyList<ImageFile> Scan(IReadOnlyList<string> sourceFolders, CancellationToken cancellationToken)
    {
        var results = new List<ImageFile>();

        foreach (var folder in sourceFolders)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!Directory.Exists(folder))
            {
                _logger.LogWarning("Source folder {Folder} does not exist, skipping.", folder);
                continue;
            }

            // One folder becoming inaccessible mid-scan (permission change, removable drive
            // unplugged) or hitting an unexpected I/O error must not lose the results already
            // found in other, unrelated source folders (Roadmap Phase 16: Zugriffsfehler).
            try
            {
                results.AddRange(EnumerateImageFiles(folder, cancellationToken).Select(ToImageFile));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                _logger.LogWarning(ex, "Failed to scan source folder {Folder}, skipping.", folder);
            }
        }

        _logger.LogInformation(
            "Scanned {FolderCount} folder(s), found {FileCount} image(s).",
            sourceFolders.Count,
            results.Count);

        return results;
    }

    private static IEnumerable<string> EnumerateImageFiles(string folder, CancellationToken cancellationToken)
    {
        var options = new EnumerationOptions
        {
            RecurseSubdirectories = true,
            IgnoreInaccessible = true,
        };

        foreach (var path in Directory.EnumerateFiles(folder, "*", options))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (SupportedImageFormats.IsSupported(path))
            {
                yield return path;
            }
        }
    }

    private static ImageFile ToImageFile(string path)
    {
        var info = new FileInfo(path);
        return new ImageFile
        {
            FullPath = path,
            FileName = info.Name,
            SizeInBytes = info.Length,
        };
    }
}
