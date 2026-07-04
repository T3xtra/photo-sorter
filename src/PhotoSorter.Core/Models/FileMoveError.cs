namespace PhotoSorter.Core.Models;

/// <summary>A single file that could not be moved, and why.</summary>
public sealed record FileMoveError(ImageFile Image, string Message);
