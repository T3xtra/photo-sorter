using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using PhotoSorter.Core.Models;
using PhotoSorter.Core.ViewModels;

namespace PhotoSorter.App.Views.Dialogs;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
    }

    public SettingsWindow(SettingsViewModel viewModel) : this()
    {
        DataContext = viewModel;
        viewModel.RequestClose += (_, _) => Close();
        AddHandler(KeyDownEvent, OnKeyDown, handledEventsToo: true);
    }

    /// <summary>
    /// While a <see cref="HotkeyBindingViewModel"/> is capturing (see its BeginCaptureCommand),
    /// the next key press becomes its new binding instead of doing anything else. Standalone
    /// modifier presses are ignored so the user can hold Ctrl/Shift/Alt before the real key.
    /// </summary>
    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not SettingsViewModel viewModel)
        {
            return;
        }

        var capturing = viewModel.HotkeyBindings.FirstOrDefault(b => b.IsCapturing);
        if (capturing is null)
        {
            return;
        }

        e.Handled = true;

        if (e.Key is Key.LeftCtrl or Key.RightCtrl or Key.LeftShift or Key.RightShift or Key.LeftAlt or Key.RightAlt)
        {
            return;
        }

        if (e.Key == Key.Escape)
        {
            capturing.CancelCaptureCommand.Execute(null);
            return;
        }

        var chord = new HotkeyChord(
            e.Key.ToString(),
            e.KeyModifiers.HasFlag(KeyModifiers.Control),
            e.KeyModifiers.HasFlag(KeyModifiers.Shift),
            e.KeyModifiers.HasFlag(KeyModifiers.Alt));

        capturing.ApplyCapturedChord(chord);
    }
}
