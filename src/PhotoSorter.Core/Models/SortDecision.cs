namespace PhotoSorter.Core.Models;

/// <summary>A recorded decision to sort <paramref name="Image"/> to <paramref name="Action"/>'s target.</summary>
public sealed record SortDecision(ImageFile Image, SortAction Action);
