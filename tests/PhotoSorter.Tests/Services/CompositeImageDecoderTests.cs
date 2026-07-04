using System;
using System.IO;
using System.Threading.Tasks;
using PhotoSorter.Core.Services.Implementations;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;

namespace PhotoSorter.Tests.Services;

public sealed class CompositeImageDecoderTests : IDisposable
{
    private readonly string _root;

    public CompositeImageDecoderTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "PhotoSorterTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    [Fact]
    public async Task DecodeToPngAsync_ForRawExtension_UsesRawPreviewDecoder()
    {
        var jpegBytes = MinimalTiffBuilder.CreateMinimalJpegBytes();
        var tiffBytes = MinimalTiffBuilder.BuildMinimalRawTiffWithEmbeddedJpegPreview(jpegBytes);
        var path = Path.Combine(_root, "sample.cr2");
        File.WriteAllBytes(path, tiffBytes);

        var sut = new CompositeImageDecoder(new RawPreviewImageDecoder(), new ImageSharpImageDecoder());

        // This synthetic file has no real TIFF pixel-strip tags, so a direct ImageSharp decode
        // (the non-RAW path) would fail; succeeding proves the RAW preview-extraction path ran.
        var pngBytes = await sut.DecodeToPngAsync(path);

        using var decoded = Image.Load(pngBytes);
        Assert.Equal(1, decoded.Width);
        Assert.Equal(1, decoded.Height);
    }

    [Fact]
    public async Task DecodeToPngAsync_ForStandardExtension_UsesImageSharpDecoder()
    {
        using var image = new Image<Rgba32>(3, 2);
        var path = Path.Combine(_root, "sample.png");
        image.Save(path, new PngEncoder());

        var sut = new CompositeImageDecoder(new RawPreviewImageDecoder(), new ImageSharpImageDecoder());

        var pngBytes = await sut.DecodeToPngAsync(path);

        using var decoded = Image.Load(pngBytes);
        Assert.Equal(3, decoded.Width);
        Assert.Equal(2, decoded.Height);
    }
}
