using System.Threading;
using System.Threading.Tasks;
using PhotoSorter.Core.Configuration;
using PhotoSorter.Core.Services.Abstractions;

namespace PhotoSorter.Core.Services.Implementations;

/// <summary>Dispatches to <see cref="RawThumbnailGenerator"/> for RAW files, <see cref="ImageSharpThumbnailGenerator"/> otherwise.</summary>
public sealed class CompositeThumbnailGenerator : IThumbnailGenerator
{
    private readonly RawThumbnailGenerator _rawGenerator;
    private readonly ImageSharpThumbnailGenerator _standardGenerator;

    public CompositeThumbnailGenerator(RawThumbnailGenerator rawGenerator, ImageSharpThumbnailGenerator standardGenerator)
    {
        _rawGenerator = rawGenerator;
        _standardGenerator = standardGenerator;
    }

    public Task<byte[]> GenerateAsync(string filePath, CancellationToken cancellationToken = default) =>
        SupportedImageFormats.IsRaw(filePath)
            ? _rawGenerator.GenerateAsync(filePath, cancellationToken)
            : _standardGenerator.GenerateAsync(filePath, cancellationToken);
}
