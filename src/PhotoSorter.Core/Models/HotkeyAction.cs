namespace PhotoSorter.Core.Models;

/// <summary>A user-triggerable action that can be bound to a key chord.</summary>
public enum HotkeyAction
{
    /// <summary>Sort the current image to the left target (Phase 8).</summary>
    SortLeft,

    /// <summary>Sort the current image to the right target (Phase 8).</summary>
    SortRight,

    PreviousImage,

    NextImage,

    /// <summary>Skip the current image without deciding (Phase 8).</summary>
    Skip,

    /// <summary>Undo the last sort decision (Phase 9).</summary>
    Undo,

    ToggleFullscreen,

    ZoomIn,

    ZoomOut,

    /// <summary>Resets zoom/pan back to "Bild einpassen" (fit to window).</summary>
    ResetZoom,
}
