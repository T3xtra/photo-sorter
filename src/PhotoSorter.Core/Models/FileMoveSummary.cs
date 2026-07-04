using System;
using System.Collections.Generic;

namespace PhotoSorter.Core.Models;

/// <summary>Result of applying a batch of sort decisions ("Bei Fehlern: Restliche Dateien weiter bearbeiten").</summary>
public sealed class FileMoveSummary
{
    public IReadOnlyList<ImageFile> SucceededImages { get; init; } = Array.Empty<ImageFile>();

    public IReadOnlyList<FileMoveError> Errors { get; init; } = Array.Empty<FileMoveError>();
}
