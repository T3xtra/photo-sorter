using System.Threading;
using System.Threading.Tasks;
using PhotoSorter.Core.Configuration;
using PhotoSorter.Core.Services.Abstractions;

namespace PhotoSorter.Core.Services.Implementations;

/// <summary>
/// Dispatches to <see cref="RawPreviewImageDecoder"/> for RAW files and
/// <see cref="ImageSharpImageDecoder"/> for everything else, based on file extension
/// (<see cref="SupportedImageFormats.IsRaw"/>). Registered as the single <see cref="IImageDecoder"/>
/// so RAW support (Phase 14) required no changes to any code that already consumes
/// <see cref="IImageDecoder"/> (ImageViewerViewModel, ImageCache, ...).
/// </summary>
public sealed class CompositeImageDecoder : IImageDecoder
{
    private readonly RawPreviewImageDecoder _rawDecoder;
    private readonly ImageSharpImageDecoder _standardDecoder;

    public CompositeImageDecoder(RawPreviewImageDecoder rawDecoder, ImageSharpImageDecoder standardDecoder)
    {
        _rawDecoder = rawDecoder;
        _standardDecoder = standardDecoder;
    }

    public Task<byte[]> DecodeToPngAsync(string filePath, CancellationToken cancellationToken = default) =>
        SupportedImageFormats.IsRaw(filePath)
            ? _rawDecoder.DecodeToPngAsync(filePath, cancellationToken)
            : _standardDecoder.DecodeToPngAsync(filePath, cancellationToken);
}
