using System;
using System.IO;
using System.Threading.Tasks;
using PhotoSorter.Core.Services.Implementations;
using SixLabors.ImageSharp;

namespace PhotoSorter.Tests.Services;

public sealed class RawPreviewTests : IDisposable
{
    private readonly string _root;

    public RawPreviewTests()
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

    private string CreateSyntheticRawFile(string fileName)
    {
        var jpegBytes = MinimalTiffBuilder.CreateMinimalJpegBytes();
        var tiffBytes = MinimalTiffBuilder.BuildMinimalRawTiffWithEmbeddedJpegPreview(jpegBytes);
        var path = Path.Combine(_root, fileName);
        File.WriteAllBytes(path, tiffBytes);
        return path;
    }

    [Fact]
    public void ExtractEmbeddedPreview_FindsTheEmbeddedJpegBytes()
    {
        var jpegBytes = MinimalTiffBuilder.CreateMinimalJpegBytes();
        var tiffBytes = MinimalTiffBuilder.BuildMinimalRawTiffWithEmbeddedJpegPreview(jpegBytes);
        var path = Path.Combine(_root, "sample.cr2");
        File.WriteAllBytes(path, tiffBytes);

        var extracted = RawPreviewReader.ExtractEmbeddedPreview(path);

        Assert.Equal(jpegBytes, extracted);
    }

    [Fact]
    public void ExtractEmbeddedPreview_WhenNoThumbnailPresent_Throws()
    {
        var path = Path.Combine(_root, "no-preview.cr2");
        // A file MetadataExtractor can't parse as TIFF at all - no ExifThumbnailDirectory possible.
        File.WriteAllBytes(path, [1, 2, 3, 4]);

        Assert.Throws<NotSupportedException>(() => RawPreviewReader.ExtractEmbeddedPreview(path));
    }

    [Fact]
    public async Task RawPreviewImageDecoder_DecodesEmbeddedPreviewToValidPng()
    {
        var path = CreateSyntheticRawFile("sample.nef");
        var sut = new RawPreviewImageDecoder();

        var pngBytes = await sut.DecodeToPngAsync(path);

        using var decoded = Image.Load(pngBytes);
        Assert.Equal(1, decoded.Width);
        Assert.Equal(1, decoded.Height);
    }

    [Fact]
    public async Task RawThumbnailGenerator_ProducesResizedPng()
    {
        var path = CreateSyntheticRawFile("sample.arw");
        var sut = new RawThumbnailGenerator();

        var pngBytes = await sut.GenerateAsync(path);

        using var decoded = Image.Load(pngBytes);
        Assert.True(decoded.Width <= 100 && decoded.Height <= 100);
    }
}
