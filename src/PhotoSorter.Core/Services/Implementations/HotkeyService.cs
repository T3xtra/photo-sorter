using System;
using System.Collections.Generic;
using PhotoSorter.Core.Models;
using PhotoSorter.Core.Services.Abstractions;

namespace PhotoSorter.Core.Services.Implementations;

/// <inheritdoc cref="IHotkeyService"/>
public sealed class HotkeyService : IHotkeyService
{
    /// <summary>Default bindings per SoftwareDesign.md / UI-Design.md.</summary>
    private static readonly IReadOnlyDictionary<HotkeyAction, HotkeyChord> Defaults = new Dictionary<HotkeyAction, HotkeyChord>
    {
        [HotkeyAction.SortLeft] = new("Left"),
        [HotkeyAction.SortRight] = new("Right"),
        [HotkeyAction.PreviousImage] = new("Up"),
        [HotkeyAction.NextImage] = new("Down"),
        [HotkeyAction.Skip] = new("Space"),
        [HotkeyAction.Undo] = new("Back"),
        [HotkeyAction.ToggleFullscreen] = new("F"),
        [HotkeyAction.ZoomIn] = new("OemPlus", Ctrl: true),
        [HotkeyAction.ZoomOut] = new("OemMinus", Ctrl: true),
    };

    private readonly ISettingsService _settingsService;
    private readonly Dictionary<HotkeyAction, HotkeyChord> _bindings;

    public HotkeyService(ISettingsService settingsService)
    {
        _settingsService = settingsService;
        _bindings = LoadFromSettingsOrDefaults();
    }

    public IReadOnlyDictionary<HotkeyAction, HotkeyChord> Bindings => _bindings;

    public HotkeyAction? Resolve(HotkeyChord chord)
    {
        foreach (var (action, bound) in _bindings)
        {
            if (bound == chord)
            {
                return action;
            }
        }

        return null;
    }

    public void SetBinding(HotkeyAction action, HotkeyChord chord)
    {
        _bindings[action] = chord;
        Persist();
    }

    public void ResetToDefaults()
    {
        _bindings.Clear();
        foreach (var (action, chord) in Defaults)
        {
            _bindings[action] = chord;
        }

        Persist();
    }

    private Dictionary<HotkeyAction, HotkeyChord> LoadFromSettingsOrDefaults()
    {
        var result = new Dictionary<HotkeyAction, HotkeyChord>(Defaults);

        foreach (var (actionName, chord) in _settingsService.Current.HotkeyBindings)
        {
            if (Enum.TryParse<HotkeyAction>(actionName, out var action))
            {
                result[action] = chord;
            }
        }

        return result;
    }

    private void Persist()
    {
        _settingsService.Current.HotkeyBindings.Clear();
        foreach (var (action, chord) in _bindings)
        {
            _settingsService.Current.HotkeyBindings[action.ToString()] = chord;
        }

        _ = _settingsService.SaveAsync();
    }
}
