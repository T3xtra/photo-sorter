using System.Collections.Generic;
using PhotoSorter.Core.Models;

namespace PhotoSorter.Core.Services.Abstractions;

/// <summary>
/// Owns the action-to-key-chord bindings ("Hotkeys müssen vollständig konfigurierbar sein").
/// Persistence and an editor UI are added in Phase 15; this phase provides the rebindable
/// infrastructure and sensible defaults.
/// </summary>
public interface IHotkeyService
{
    IReadOnlyDictionary<HotkeyAction, HotkeyChord> Bindings { get; }

    /// <summary>Returns the action bound to the given chord, or null if none.</summary>
    HotkeyAction? Resolve(HotkeyChord chord);

    void SetBinding(HotkeyAction action, HotkeyChord chord);

    void ResetToDefaults();
}
