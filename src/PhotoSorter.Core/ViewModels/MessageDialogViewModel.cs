using System;
using CommunityToolkit.Mvvm.Input;

namespace PhotoSorter.Core.ViewModels;

/// <summary>ViewModel for a simple message dialog with a single OK button.</summary>
public sealed partial class MessageDialogViewModel : ViewModelBase
{
    public MessageDialogViewModel(string title, string message)
    {
        Title = title;
        Message = message;
    }

    public string Title { get; }

    public string Message { get; }

    public event EventHandler? RequestClose;

    [RelayCommand]
    private void Ok() => RequestClose?.Invoke(this, EventArgs.Empty);
}
