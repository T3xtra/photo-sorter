namespace PhotoSorter.Core.Models;

/// <summary>An image file discovered in a source folder.</summary>
public sealed class ImageFile
{
    public required string FullPath { get; init; }

    public required string FileName { get; init; }

    public required long SizeInBytes { get; init; }
}
