using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PhotoSorter.Core.Models;
using PhotoSorter.Core.Services.Abstractions;

namespace PhotoSorter.Core.ViewModels;

/// <summary>
/// ViewModel for the "Einstellungen" window: Hotkeys, Dark Mode (informational, see
/// <c>docs/architecture-decisions.md</c> point 5), "Animationen ein/aus", and the "Zielordner"
/// tab (<see cref="TargetFoldersViewModel"/>). Constructed fresh each time the dialog opens (see
/// <see cref="ISettingsDialogService"/>), so it always reflects the current
/// <see cref="IHotkeyService"/>/<see cref="ISettingsService"/> state.
/// </summary>
public sealed partial class SettingsViewModel : ViewModelBase
{
    private readonly ISettingsService _settingsService;
    private readonly IHotkeyService _hotkeyService;

    public SettingsViewModel(ISettingsService settingsService, IHotkeyService hotkeyService, TargetFoldersViewModel targetFolders)
    {
        _settingsService = settingsService;
        _hotkeyService = hotkeyService;
        _animationsEnabled = settingsService.Current.AnimationsEnabled;
        TargetFolders = targetFolders;

        var displayNames = HotkeyActionDisplayNames.Ordered.ToDictionary(entry => entry.Action, entry => entry.DisplayName);
        HotkeyBindings = HotkeyActionDisplayNames.Ordered
            .Select(entry => new HotkeyBindingViewModel(entry.Action, entry.DisplayName, hotkeyService, action => displayNames[action]))
            .ToList();
    }

    /// <summary>"Zielordner" tab: left/right sort target selection (moved here from the toolbar).</summary>
    public TargetFoldersViewModel TargetFolders { get; }

    /// <summary>"Dark Mode" is fixed per architecture decision 5 - shown as an informational, disabled toggle.</summary>
    public bool DarkModeEnabled => true;

    [ObservableProperty]
    private bool _animationsEnabled;

    public IReadOnlyList<HotkeyBindingViewModel> HotkeyBindings { get; }

    /// <summary>Raised once the window should close.</summary>
    public event EventHandler? RequestClose;

    partial void OnAnimationsEnabledChanged(bool value)
    {
        _settingsService.Current.AnimationsEnabled = value;
        _ = _settingsService.SaveAsync();
    }

    [RelayCommand]
    private void ResetHotkeys()
    {
        _hotkeyService.ResetToDefaults();
        foreach (var binding in HotkeyBindings)
        {
            binding.RefreshFromService();
        }
    }

    [RelayCommand]
    private void Close() => RequestClose?.Invoke(this, EventArgs.Empty);
}
