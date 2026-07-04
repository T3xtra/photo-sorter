namespace PhotoSorter.Core.Models;

/// <summary>
/// A sort target: either a real folder, or the trash (SoftwareDesign.md: "Der linke Zielordner
/// darf alternativ der Papierkorb sein" - only the left target may be the trash; that constraint
/// is enforced by the UI, not this type).
/// </summary>
public sealed record TargetFolder(bool IsTrash, string? Path)
{
    public static TargetFolder Trash() => new(true, null);

    public static TargetFolder At(string path) => new(false, path);
}
