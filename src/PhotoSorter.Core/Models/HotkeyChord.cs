using System.Collections.Generic;

namespace PhotoSorter.Core.Models;

/// <summary>
/// A key combination. <paramref name="Key"/> deliberately uses Avalonia's <c>Key</c> enum member
/// names as plain strings (e.g. "Left", "OemPlus") rather than referencing the Avalonia type
/// directly, so Core stays UI-framework-free; the View layer converts with a trivial
/// <c>Enum.Parse&lt;Key&gt;</c>/<c>ToString()</c> round-trip.
/// </summary>
public sealed record HotkeyChord(string Key, bool Ctrl = false, bool Shift = false, bool Alt = false)
{
    public override string ToString()
    {
        var parts = new List<string>();
        if (Ctrl)
        {
            parts.Add("Ctrl");
        }

        if (Shift)
        {
            parts.Add("Shift");
        }

        if (Alt)
        {
            parts.Add("Alt");
        }

        parts.Add(Key);
        return string.Join("+", parts);
    }
}
