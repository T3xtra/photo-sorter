using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PhotoSorter.Core.Services.Abstractions;

namespace PhotoSorter.Tests.TestDoubles;

/// <summary>An <see cref="IImageCache"/> that returns a fixed byte array per requested path, without real decoding.</summary>
public sealed class FakeImageCache : IImageCache
{
    public byte[] Result { get; set; } = [1, 2, 3];

    /// <summary>When set, <see cref="GetAsync"/> fails with this exception instead of returning <see cref="Result"/>.</summary>
    public Exception? ExceptionToThrow { get; set; }

    public List<string> RequestedPaths { get; } = [];

    public List<IReadOnlyList<string>> WindowUpdates { get; } = [];

    public Task<byte[]> GetAsync(string path, CancellationToken cancellationToken = default)
    {
        RequestedPaths.Add(path);
        return ExceptionToThrow is not null ? Task.FromException<byte[]>(ExceptionToThrow) : Task.FromResult(Result);
    }

    public void UpdateWindow(IReadOnlyList<string> pathsToKeep) => WindowUpdates.Add(pathsToKeep);
}
