namespace PhotoSorter.Core.Models;

/// <summary>Counts shown in the "Sortierung abgeschlossen" confirmation (UI-Design.md).</summary>
public sealed record ApplySummary(int LeftCount, int RightCount, int SkippedCount);
