using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PhotoSorter.Core.Models;
using PhotoSorter.Core.Services.Abstractions;

namespace PhotoSorter.Tests.TestDoubles;

/// <summary>An <see cref="IFolderScannerService"/> that returns a fixed, pre-set result.</summary>
public sealed class FakeFolderScannerService : IFolderScannerService
{
    public IReadOnlyList<ImageFile> Result { get; set; } = [];

    public IReadOnlyList<string>? LastRequestedFolders { get; private set; }

    public Task<IReadOnlyList<ImageFile>> ScanAsync(IReadOnlyList<string> sourceFolders, CancellationToken cancellationToken = default)
    {
        LastRequestedFolders = sourceFolders;
        return Task.FromResult(Result);
    }
}
