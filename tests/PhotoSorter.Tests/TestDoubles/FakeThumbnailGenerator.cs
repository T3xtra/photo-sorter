using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PhotoSorter.Core.Services.Abstractions;

namespace PhotoSorter.Tests.TestDoubles;

/// <summary>An <see cref="IThumbnailGenerator"/> that returns a fixed byte array per requested path.</summary>
public sealed class FakeThumbnailGenerator : IThumbnailGenerator
{
    public byte[] Result { get; set; } = [9, 9, 9];

    /// <summary>When set, generation fails for paths in <see cref="FailingPaths"/> (or every path, if empty).</summary>
    public Exception? ExceptionToThrow { get; set; }

    public HashSet<string> FailingPaths { get; } = [];

    public List<string> RequestedPaths { get; } = [];

    public Task<byte[]> GenerateAsync(string filePath, CancellationToken cancellationToken = default)
    {
        lock (RequestedPaths)
        {
            RequestedPaths.Add(filePath);
        }

        if (ExceptionToThrow is not null && (FailingPaths.Count == 0 || FailingPaths.Contains(filePath)))
        {
            return Task.FromException<byte[]>(ExceptionToThrow);
        }

        return Task.FromResult(Result);
    }
}
