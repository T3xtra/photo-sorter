using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PhotoSorter.Core.Services.Abstractions;

namespace PhotoSorter.Tests.TestDoubles;

/// <summary>An <see cref="IImageDecoder"/> that returns a fixed byte array per requested path.</summary>
public sealed class FakeImageDecoder : IImageDecoder
{
    public byte[] Result { get; set; } = [1, 2, 3];

    public List<string> RequestedPaths { get; } = [];

    public Task<byte[]> DecodeToPngAsync(string filePath, CancellationToken cancellationToken = default)
    {
        RequestedPaths.Add(filePath);
        return Task.FromResult(Result);
    }
}
