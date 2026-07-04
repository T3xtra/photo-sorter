using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PhotoSorter.Core.Models;

namespace PhotoSorter.Core.Services.Abstractions;

/// <summary>Recursively finds supported image files within a set of source folders.</summary>
public interface IFolderScannerService
{
    Task<IReadOnlyList<ImageFile>> ScanAsync(IReadOnlyList<string> sourceFolders, CancellationToken cancellationToken = default);
}
