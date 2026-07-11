using System.Collections.Generic;

namespace PhotoSorter.Core.Models;

/// <summary>
/// German display names for each <see cref="HotkeyAction"/>, in the order shown in both the
/// Hotkeys settings section (<c>SettingsViewModel</c>) and the "Hilfe" shortcuts overview
/// (<c>ToolbarViewModel</c>) - kept in one place so the two never drift apart.
/// </summary>
public static class HotkeyActionDisplayNames
{
    public static readonly IReadOnlyList<(HotkeyAction Action, string DisplayName)> Ordered =
    [
        (HotkeyAction.SortLeft, "Nach links sortieren"),
        (HotkeyAction.SortRight, "Nach rechts sortieren"),
        (HotkeyAction.PreviousImage, "Vorheriges Bild"),
        (HotkeyAction.NextImage, "Nächstes Bild"),
        (HotkeyAction.Skip, "Überspringen"),
        (HotkeyAction.Undo, "Rückgängig"),
        (HotkeyAction.ToggleFullscreen, "Vollbild umschalten"),
        (HotkeyAction.ZoomIn, "Vergrößern"),
        (HotkeyAction.ZoomOut, "Verkleinern"),
        (HotkeyAction.ResetZoom, "Zoom zurücksetzen"),
    ];
}
