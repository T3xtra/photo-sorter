using System.IO;
using System.Threading;
using System.Threading.Tasks;
using PhotoSorter.Core.Services.Abstractions;
using SixLabors.ImageSharp;

namespace PhotoSorter.Core.Services.Implementations;

/// <inheritdoc cref="IImageDecoder"/>
public sealed class ImageSharpImageDecoder : IImageDecoder
{
    public async Task<byte[]> DecodeToPngAsync(string filePath, CancellationToken cancellationToken = default)
    {
        using var image = await Image.LoadAsync(filePath, cancellationToken).ConfigureAwait(false);

        var output = new MemoryStream();
        await image.SaveAsPngAsync(output, cancellationToken).ConfigureAwait(false);
        return output.ToArray();
    }
}
