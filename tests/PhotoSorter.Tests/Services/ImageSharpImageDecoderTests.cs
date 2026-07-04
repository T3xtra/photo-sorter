using System;
using System.IO;
using System.Threading.Tasks;
using PhotoSorter.Core.Services.Implementations;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Tiff;
using SixLabors.ImageSharp.PixelFormats;

namespace PhotoSorter.Tests.Services;

public sealed class ImageSharpImageDecoderTests : IDisposable
{
    private readonly string _root;

    public ImageSharpImageDecoderTests()
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

    [Theory]
    [InlineData("sample.png")]
    [InlineData("sample.jpg")]
    [InlineData("sample.bmp")]
    [InlineData("sample.tif")] // Mandatory per SoftwareDesign.md; the main reason ImageSharp is used at all.
    public async Task DecodeToPngAsync_ProducesValidPng_ForEachMandatoryFormat(string fileName)
    {
        var path = Path.Combine(_root, fileName);
        CreateTestImage(path);

        var sut = new ImageSharpImageDecoder();

        var pngBytes = await sut.DecodeToPngAsync(path);

        Assert.NotEmpty(pngBytes);
        using var decoded = Image.Load(pngBytes);
        Assert.Equal(4, decoded.Width);
        Assert.Equal(3, decoded.Height);
        AssertIsPng(pngBytes);
    }

    private static void CreateTestImage(string path)
    {
        using var image = new Image<Rgba32>(4, 3);
        for (var y = 0; y < image.Height; y++)
        {
            for (var x = 0; x < image.Width; x++)
            {
                image[x, y] = new Rgba32(100, 149, 237, 255);
            }
        }

        switch (Path.GetExtension(path).ToLowerInvariant())
        {
            case ".png":
                image.Save(path, new PngEncoder());
                break;
            case ".jpg":
            case ".jpeg":
                image.Save(path, new JpegEncoder());
                break;
            case ".bmp":
                image.Save(path, new BmpEncoder());
                break;
            case ".tif":
            case ".tiff":
                image.Save(path, new TiffEncoder());
                break;
            default:
                throw new NotSupportedException($"Unsupported test extension for {path}.");
        }
    }

    private static void AssertIsPng(byte[] bytes)
    {
        ReadOnlySpan<byte> pngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
        Assert.True(bytes.AsSpan(0, 8).SequenceEqual(pngSignature));
    }
}
