using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;

namespace PhotoSorter.Core.Services.Implementations;

/// <summary>
/// Extracts the embedded preview JPEG from a RAW file. Shared by <see cref="RawPreviewImageDecoder"/>
/// (full display) and <see cref="RawThumbnailGenerator"/> (thumbnail strip) so both decode the
/// same embedded image instead of duplicating the extraction logic.
/// </summary>
internal static class RawPreviewReader
{
    /// <exception cref="NotSupportedException">
    /// The file couldn't be parsed at all, or no embedded preview could be located within it
    /// (e.g. CR3 and RW2 aren't supported by the underlying MetadataExtractor library - see
    /// docs/architecture-decisions.md).
    /// </exception>
    public static byte[] ExtractEmbeddedPreview(string filePath)
    {
        IReadOnlyList<MetadataExtractor.Directory> directories;
        try
        {
            using var stream = File.OpenRead(filePath);
            directories = ImageMetadataReader.ReadMetadata(stream);
        }
        catch (ImageProcessingException ex)
        {
            throw new NotSupportedException($"Could not read metadata from RAW file: {filePath}", ex);
        }

        var thumbnailDirectory = directories.OfType<ExifThumbnailDirectory>().FirstOrDefault();
        if (thumbnailDirectory is not null
            && thumbnailDirectory.TryGetInt32(ExifThumbnailDirectory.TagThumbnailOffset, out var offset)
            && thumbnailDirectory.TryGetInt32(ExifThumbnailDirectory.TagThumbnailLength, out var length)
            && length > 0)
        {
            using var stream = File.OpenRead(filePath);
            var buffer = new byte[length];
            stream.Seek(offset, SeekOrigin.Begin);
            var read = stream.Read(buffer, 0, length);
            if (read == length)
            {
                return buffer;
            }
        }

        throw new NotSupportedException($"No embedded preview found in RAW file: {filePath}");
    }
}
