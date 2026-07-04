using System;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;

namespace PhotoSorter.Tests.Services;

/// <summary>
/// Builds a minimal, synthetic, bare little-endian TIFF file with an embedded JPEG preview in a
/// follower IFD (IFD0 -&gt; IFD1, tags 0x0201/0x0202) - the same shape as CR2/NEF/ARW/DNG/ORF RAW
/// files, which are themselves bare TIFF containers. Lets <c>RawPreviewReader</c> be tested
/// without needing a real camera RAW file.
/// </summary>
internal static class MinimalTiffBuilder
{
    private const ushort TypeShort = 3;
    private const ushort TypeLong = 4;

    public static byte[] CreateMinimalJpegBytes()
    {
        using var image = new Image<Rgba32>(1, 1);
        image[0, 0] = new Rgba32(255, 0, 0, 255);

        using var ms = new MemoryStream();
        image.Save(ms, new JpegEncoder());
        return ms.ToArray();
    }

    public static byte[] BuildMinimalRawTiffWithEmbeddedJpegPreview(byte[] jpegBytes)
    {
        if (jpegBytes is null || jpegBytes.Length == 0)
        {
            throw new ArgumentException("JPEG bytes must not be null/empty.", nameof(jpegBytes));
        }

        const int headerSize = 8; // "II" + magic(42) + IFD0 offset
        const int ifd0Offset = headerSize;

        const int ifd0EntryCount = 1;
        const int ifd0Size = 2 + (ifd0EntryCount * 12) + 4; // count + entries + nextIfdOffset
        var ifd1Offset = ifd0Offset + ifd0Size;

        const int ifd1EntryCount = 2;
        const int ifd1Size = 2 + (ifd1EntryCount * 12) + 4;
        var jpegOffset = ifd1Offset + ifd1Size;

        using var ms = new MemoryStream();
        using var w = new BinaryWriter(ms); // BinaryWriter always writes little-endian, matching "II"

        // ---- TIFF header ----
        w.Write((byte)'I');
        w.Write((byte)'I');
        w.Write((ushort)42);
        w.Write((uint)ifd0Offset);

        // ---- IFD0 ----
        w.Write((ushort)ifd0EntryCount);

        // Tag 0x0100 ImageWidth, type SHORT, count 1, value 1.
        w.Write((ushort)0x0100);
        w.Write(TypeShort);
        w.Write((uint)1);
        w.Write((ushort)1);
        w.Write((ushort)0); // padding to fill the 4-byte value/offset slot

        w.Write((uint)ifd1Offset); // offset to next IFD (IFD1)

        // ---- IFD1 ----
        w.Write((ushort)ifd1EntryCount);

        // Tag 0x0201 JPEGInterchangeFormat, type LONG, count 1, value = jpegOffset.
        w.Write((ushort)0x0201);
        w.Write(TypeLong);
        w.Write((uint)1);
        w.Write((uint)jpegOffset);

        // Tag 0x0202 JPEGInterchangeFormatLength, type LONG, count 1, value = jpegBytes.Length.
        w.Write((ushort)0x0202);
        w.Write(TypeLong);
        w.Write((uint)1);
        w.Write((uint)jpegBytes.Length);

        w.Write((uint)0); // no next IFD after IFD1

        // ---- Embedded JPEG preview ----
        w.Write(jpegBytes);

        w.Flush();
        return ms.ToArray();
    }
}
