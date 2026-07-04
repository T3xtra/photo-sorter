using System.IO;
using System.Threading;
using System.Threading.Tasks;
using PhotoSorter.Core.Services.Abstractions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace PhotoSorter.Core.Services.Implementations;

/// <inheritdoc cref="IThumbnailGenerator"/>
public sealed class ImageSharpThumbnailGenerator : IThumbnailGenerator
{
    /// <summary>Matches the "ca. 100 x 100 Pixel" thumbnail size from UI-Design.md.</summary>
    private const int ThumbnailSize = 100;

    public async Task<byte[]> GenerateAsync(string filePath, CancellationToken cancellationToken = default)
    {
        using var image = await Image.LoadAsync(filePath, cancellationToken).ConfigureAwait(false);

        image.Mutate(ctx => ctx.Resize(new ResizeOptions
        {
            Size = new Size(ThumbnailSize, ThumbnailSize),
            Mode = ResizeMode.Max,
        }));

        var output = new MemoryStream();
        await image.SaveAsPngAsync(output, cancellationToken).ConfigureAwait(false);
        return output.ToArray();
    }
}
