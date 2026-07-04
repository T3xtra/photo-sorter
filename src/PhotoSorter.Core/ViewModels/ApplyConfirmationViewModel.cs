using System;
using CommunityToolkit.Mvvm.Input;

namespace PhotoSorter.Core.ViewModels;

/// <summary>ViewModel for the "Sortierung abgeschlossen" confirmation dialog (UI-Design.md).</summary>
public sealed partial class ApplyConfirmationViewModel : ViewModelBase
{
    public ApplyConfirmationViewModel(int leftCount, int rightCount, int skippedCount)
    {
        LeftCount = leftCount;
        RightCount = rightCount;
        SkippedCount = skippedCount;
    }

    public int LeftCount { get; }

    public int RightCount { get; }

    public int SkippedCount { get; }

    /// <summary>True if "Anwenden" was chosen, false for "Abbrechen". Null until the user decides.</summary>
    public bool? Result { get; private set; }

    /// <summary>Raised once <see cref="Result"/> is set; the View closes the window in response.</summary>
    public event EventHandler? RequestClose;

    [RelayCommand]
    private void Apply()
    {
        Result = true;
        RequestClose?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void Cancel()
    {
        Result = false;
        RequestClose?.Invoke(this, EventArgs.Empty);
    }
}
