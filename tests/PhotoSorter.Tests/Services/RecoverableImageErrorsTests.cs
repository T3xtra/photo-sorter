using System;
using System.IO;
using PhotoSorter.Core.Services.Implementations;
using SixLabors.ImageSharp;

namespace PhotoSorter.Tests.Services;

public sealed class RecoverableImageErrorsTests
{
    [Theory]
    [InlineData(typeof(FileNotFoundException))]
    [InlineData(typeof(DirectoryNotFoundException))]
    [InlineData(typeof(IOException))]
    [InlineData(typeof(UnauthorizedAccessException))]
    [InlineData(typeof(NotSupportedException))]
    [InlineData(typeof(UnknownImageFormatException))]
    [InlineData(typeof(InvalidImageContentException))]
    public void IsRecoverable_ReturnsTrue_ForExpectedExceptionTypes(Type exceptionType)
    {
        var exception = (Exception)Activator.CreateInstance(exceptionType, "test")!;

        Assert.True(RecoverableImageErrors.IsRecoverable(exception));
    }

    [Fact]
    public void IsRecoverable_ReturnsFalse_ForUnrelatedExceptions()
    {
        Assert.False(RecoverableImageErrors.IsRecoverable(new InvalidOperationException()));
        Assert.False(RecoverableImageErrors.IsRecoverable(new ArgumentException()));
    }
}
