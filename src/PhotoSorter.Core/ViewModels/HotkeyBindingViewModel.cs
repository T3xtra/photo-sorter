using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PhotoSorter.Core.Models;
using PhotoSorter.Core.Services.Abstractions;

namespace PhotoSorter.Core.ViewModels;

/// <summary>
/// One rebindable row in the Hotkeys settings section. The View starts a capture via
/// <see cref="BeginCaptureCommand"/>, then feeds the next key press to
/// <see cref="ApplyCapturedChord"/> once it's turned into a <see cref="HotkeyChord"/>
/// (that translation needs Avalonia's Key enum, so it happens in the View's code-behind).
/// </summary>
public sealed partial class HotkeyBindingViewModel : ViewModelBase
{
    private readonly IHotkeyService _hotkeyService;
    private readonly Func<HotkeyAction, string> _displayNameLookup;

    public HotkeyBindingViewModel(HotkeyAction action, string displayName, IHotkeyService hotkeyService, Func<HotkeyAction, string> displayNameLookup)
    {
        Action = action;
        DisplayName = displayName;
        _hotkeyService = hotkeyService;
        _displayNameLookup = displayNameLookup;
        _chord = hotkeyService.Bindings[action];
    }

    public HotkeyAction Action { get; }

    public string DisplayName { get; }

    [ObservableProperty]
    private HotkeyChord _chord;

    /// <summary>True while the View is waiting for the next key press to bind.</summary>
    [ObservableProperty]
    private bool _isCapturing;

    /// <summary>Set when a captured chord is already bound to a different action; the capture is rejected.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasConflict))]
    private string? _conflictMessage;

    public bool HasConflict => ConflictMessage is not null;

    [RelayCommand]
    private void BeginCapture()
    {
        IsCapturing = true;
        ConflictMessage = null;
    }

    [RelayCommand]
    private void CancelCapture()
    {
        IsCapturing = false;
        ConflictMessage = null;
    }

    /// <summary>
    /// Applies a freshly captured chord, unless it's already bound to a different action -
    /// "Hotkeys müssen vollständig konfigurierbar sein" doesn't mean silently stealing another
    /// action's binding, so conflicts are surfaced instead of overwritten.
    /// </summary>
    public void ApplyCapturedChord(HotkeyChord chord)
    {
        var existingAction = _hotkeyService.Resolve(chord);
        if (existingAction is not null && existingAction != Action)
        {
            ConflictMessage = $"Bereits belegt durch \"{_displayNameLookup(existingAction.Value)}\".";
            return;
        }

        _hotkeyService.SetBinding(Action, chord);
        Chord = chord;
        IsCapturing = false;
        ConflictMessage = null;
    }

    /// <summary>Re-reads the bound chord from <see cref="IHotkeyService"/> (e.g. after a reset-to-defaults).</summary>
    public void RefreshFromService()
    {
        Chord = _hotkeyService.Bindings[Action];
        IsCapturing = false;
        ConflictMessage = null;
    }
}
