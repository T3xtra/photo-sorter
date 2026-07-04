using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using PhotoSorter.Core.Services.Abstractions;

namespace PhotoSorter.Core.Services.Implementations;

/// <summary>
/// Decodes RAW files (CR2/CR3, NEF, ARW, RAF, DNG, ORF, RW2) via their embedded preview JPEG
/// rather than full demosaicing ("RAW-Dateien sollen mindestens über das eingebettete Preview
/// angezeigt werden", SoftwareDesign.md). Most camera RAW formats are TIFF-based containers that
/// carry a full-size or thumbnail-size JPEG alongside the sensor data specifically so cameras and
/// viewers can show a preview without decoding the raw sensor data.
/// </summary>
public sealed class RawPreviewImageDecoder : IImageDecoder
{
    public async Task<byte[]> DecodeToPngAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var previewBytes = await Task.Run(() => RawPreviewReader.ExtractEmbeddedPreview(filePath), cancellationToken).ConfigureAwait(false);

        // Re-encode through ImageSharp so the result is guaranteed-decodable PNG bytes, exactly
        // like ImageSharpImageDecoder - the rest of the app never needs to know a RAW file was involved.
        using var image = await Image.LoadAsync(new MemoryStream(previewBytes), cancellationToken).ConfigureAwait(false);
        var output = new MemoryStream();
        await image.SaveAsPngAsync(output, cancellationToken).ConfigureAwait(false);
        return output.ToArray();
    }
}
