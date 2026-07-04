using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PhotoSorter.Core.Configuration;

/// <summary>
/// Single source of truth for which file extensions PhotoSorter recognizes as images, and which
/// of those are RAW (decoded via their embedded preview - see <c>RawPreviewImageDecoder</c>,
/// Phase 14 - rather than full demosaicing).
/// </summary>
public static class SupportedImageFormats
{
    /// <summary>Mandatory formats per the specification.</summary>
    public static readonly IReadOnlySet<string> Mandatory = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp", ".bmp", ".tif", ".tiff",
    };

    /// <summary>Optional formats supported without an additional decoder.</summary>
    public static readonly IReadOnlySet<string> Optional = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ".gif",
    };

    /// <summary>
    /// Optional RAW formats (SoftwareDesign.md: "RAW-Dateien sollen mindestens über das
    /// eingebettete Preview angezeigt werden"). Canon, Nikon, Sony, Fuji, Olympus, Panasonic, Adobe DNG.
    /// </summary>
    public static readonly IReadOnlySet<string> Raw = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ".cr2", ".cr3", ".nef", ".arw", ".raf", ".dng", ".orf", ".rw2",
    };

    public static readonly IReadOnlySet<string> All =
        new HashSet<string>(Mandatory.Concat(Optional).Concat(Raw), StringComparer.OrdinalIgnoreCase);

    public static bool IsSupported(string filePath) => All.Contains(Path.GetExtension(filePath));

    public static bool IsRaw(string filePath) => Raw.Contains(Path.GetExtension(filePath));
}
