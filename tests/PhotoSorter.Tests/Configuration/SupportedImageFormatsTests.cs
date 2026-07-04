using PhotoSorter.Core.Configuration;

namespace PhotoSorter.Tests.Configuration;

public sealed class SupportedImageFormatsTests
{
    [Theory]
    [InlineData("photo.jpg")]
    [InlineData("photo.JPEG")]
    [InlineData("photo.png")]
    [InlineData("photo.webp")]
    [InlineData("photo.bmp")]
    [InlineData("photo.tif")]
    [InlineData("photo.TIFF")]
    [InlineData("photo.gif")]
    public void IsSupported_ForKnownExtensions_ReturnsTrue(string fileName)
    {
        Assert.True(SupportedImageFormats.IsSupported(fileName));
    }

    [Theory]
    [InlineData("document.txt")]
    [InlineData("archive.zip")]
    [InlineData("noextension")]
    [InlineData("video.mp4")]
    public void IsSupported_ForUnknownExtensions_ReturnsFalse(string fileName)
    {
        Assert.False(SupportedImageFormats.IsSupported(fileName));
    }

    [Theory]
    [InlineData("photo.cr2")]
    [InlineData("photo.CR3")]
    [InlineData("photo.nef")]
    [InlineData("photo.arw")]
    [InlineData("photo.raf")]
    [InlineData("photo.dng")]
    [InlineData("photo.orf")]
    [InlineData("photo.rw2")]
    public void IsRaw_ForKnownRawExtensions_ReturnsTrue(string fileName)
    {
        Assert.True(SupportedImageFormats.IsRaw(fileName));
        Assert.True(SupportedImageFormats.IsSupported(fileName));
    }

    [Fact]
    public void IsRaw_ForStandardFormat_ReturnsFalse()
    {
        Assert.False(SupportedImageFormats.IsRaw("photo.jpg"));
    }
}
