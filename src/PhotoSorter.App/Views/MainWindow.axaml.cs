using System;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Input;
using Microsoft.Extensions.DependencyInjection;
using PhotoSorter.Core.Models;
using PhotoSorter.Core.Services.Abstractions;
using PhotoSorter.Core.ViewModels;

namespace PhotoSorter.App.Views;

public partial class MainWindow : Window
{
    private readonly IHotkeyService _hotkeyService;

    public MainWindow()
    {
        InitializeComponent();
        _hotkeyService = App.Services.GetRequiredService<IHotkeyService>();
        AddHandler(KeyDownEvent, OnKeyDown, handledEventsToo: false);
    }

    /// <summary>
    /// Resolves the pressed key combination via the configurable <see cref="IHotkeyService"/>
    /// and dispatches it to the corresponding ViewModel command.
    /// </summary>
    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        var chord = new HotkeyChord(
            e.Key.ToString(),
            e.KeyModifiers.HasFlag(KeyModifiers.Control),
            e.KeyModifiers.HasFlag(KeyModifiers.Shift),
            e.KeyModifiers.HasFlag(KeyModifiers.Alt));

        // Ctrl+Z is always Undo in addition to the configurable binding (default: Backspace) -
        // SoftwareDesign.md: "Backspace oder Strg+Z macht die letzte Aktion rückgängig". Only
        // Undo gets this fixed alias; every other action has exactly one configurable chord.
        if (chord is { Key: "Z", Ctrl: true, Shift: false, Alt: false })
        {
            e.Handled = Execute(viewModel.ImageViewerViewModel.UndoCommand);
            return;
        }

        var handled = _hotkeyService.Resolve(chord) switch
        {
            HotkeyAction.PreviousImage => Execute(viewModel.ImageViewerViewModel.PreviousImageCommand),
            HotkeyAction.NextImage => Execute(viewModel.ImageViewerViewModel.NextImageCommand),
            HotkeyAction.SortLeft => Execute(viewModel.ImageViewerViewModel.SortLeftCommand),
            HotkeyAction.SortRight => Execute(viewModel.ImageViewerViewModel.SortRightCommand),
            HotkeyAction.Skip => Execute(viewModel.ImageViewerViewModel.SkipCommand),
            HotkeyAction.Undo => Execute(viewModel.ImageViewerViewModel.UndoCommand),
            HotkeyAction.ZoomIn => Execute(() => viewModel.ImageViewerViewModel.ApplyZoomDelta(1)),
            HotkeyAction.ZoomOut => Execute(() => viewModel.ImageViewerViewModel.ApplyZoomDelta(-1)),
            HotkeyAction.ResetZoom => Execute(viewModel.ImageViewerViewModel.ResetZoomCommand),
            HotkeyAction.ToggleFullscreen => Execute(ToggleFullscreen),
            _ => false,
        };

        e.Handled = handled;
    }

    private void ToggleFullscreen() =>
        WindowState = WindowState == WindowState.FullScreen ? WindowState.Normal : WindowState.FullScreen;

    private static bool Execute(ICommand command)
    {
        command.Execute(null);
        return true;
    }

    private static bool Execute(Action action)
    {
        action();
        return true;
    }
}
