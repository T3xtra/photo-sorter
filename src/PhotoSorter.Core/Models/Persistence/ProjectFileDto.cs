using System.Collections.Generic;

namespace PhotoSorter.Core.Models.Persistence;

/// <summary>
/// The on-disk shape of a <c>.photosort</c> project file. Deliberately separate from
/// <see cref="Project"/> (which has private/internal setters for its invariants) so the file
/// format doesn't have to track the in-memory model's shape 1:1.
///
/// Settings are intentionally not embedded here, unlike the literal SoftwareDesign.md wording
/// ("Sie enthält: ... Einstellungen") - see docs/architecture-decisions.md, Punkt 17.
/// </summary>
public sealed class ProjectFileDto
{
    public List<string> SourceFolders { get; set; } = [];

    /// <summary>Full paths in their original order ("Reihenfolge").</summary>
    public List<string> ImagePaths { get; set; } = [];

    public TargetFolderDto? LeftTarget { get; set; }

    public TargetFolderDto? RightTarget { get; set; }

    public List<DecisionDto> Decisions { get; set; } = [];

    public int CurrentIndex { get; set; } = -1;
}

public sealed class TargetFolderDto
{
    public bool IsTrash { get; set; }

    public string? Path { get; set; }
}

public sealed class DecisionDto
{
    public string ImagePath { get; set; } = string.Empty;

    public SortAction Action { get; set; }
}
