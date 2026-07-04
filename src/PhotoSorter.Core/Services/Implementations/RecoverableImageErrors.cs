using System;
using System.IO;
using SixLabors.ImageSharp;

namespace PhotoSorter.Core.Services.Implementations;

/// <summary>
/// Classifies exceptions from decoding/generating a single image as "this one image is
/// unusable" (corrupted file, missing file, permission denied, unrecognized format) rather than
/// a programming error. Shared by every place that decodes images in bulk
/// (<see cref="ImageCache"/>, <see cref="PhotoSorter.Core.ViewModels.ImageViewerViewModel"/>,
/// <see cref="PhotoSorter.Core.ViewModels.ThumbnailBarViewModel"/>) so the reaction to
/// "Roadmap Phase 16: beschädigte Bilder, fehlende Dateien, Zugriffsfehler" - log a warning and
/// keep going with the rest of the project - stays in one place instead of drifting apart.
/// </summary>
internal static class RecoverableImageErrors
{
    /// <summary>
    /// <see cref="IOException"/> covers missing files (<see cref="FileNotFoundException"/>,
    /// <see cref="DirectoryNotFoundException"/>) and general I/O failures.
    /// <see cref="UnauthorizedAccessException"/> is deliberately separate - it does NOT derive
    /// from <see cref="IOException"/> - and covers permission-denied/locked files.
    /// <see cref="ImageFormatException"/> is ImageSharp's base for both an unrecognized format
    /// (<see cref="UnknownImageFormatException"/>) and genuinely corrupted content
    /// (<c>InvalidImageContentException</c>). <see cref="NotSupportedException"/> covers RAW
    /// files whose embedded preview couldn't be extracted (<see cref="RawPreviewReader"/>).
    /// </summary>
    public static bool IsRecoverable(Exception ex) =>
        ex is IOException or UnauthorizedAccessException or NotSupportedException or ImageFormatException;
}
