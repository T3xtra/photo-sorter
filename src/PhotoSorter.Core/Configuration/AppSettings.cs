using System;
using System.Collections.Generic;
using PhotoSorter.Core.Models;

namespace PhotoSorter.Core.Configuration;

/// <summary>
/// Persistent, user-editable application settings. Extended incrementally as
/// features that need persisted state (folders, hotkeys, zoom, animations) are implemented.
/// </summary>
public sealed class AppSettings
{
    public int WindowWidth { get; set; } = 1280;

    public int WindowHeight { get; set; } = 800;

    /// <summary>"Animationen ein/aus" (SoftwareDesign.md).</summary>
    public bool AnimationsEnabled { get; set; } = true;

    /// <summary>
    /// Hotkey bindings, keyed by <see cref="HotkeyAction"/> name. Empty until the user rebinds
    /// something - <c>HotkeyService</c> falls back to its built-in defaults for anything missing.
    /// </summary>
    public Dictionary<string, HotkeyChord> HotkeyBindings { get; set; } = new();

    /// <summary>"Ordner merken" (Roadmap Phase 15): last source folders, offered as the folder picker's starting location next time.</summary>
    public List<string> LastSourceFolders { get; set; } = [];

    public bool LastLeftTargetIsTrash { get; set; }

    public string? LastLeftTargetPath { get; set; }

    public string? LastRightTargetPath { get; set; }
}
