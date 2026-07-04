using Avalonia.Controls;
using PhotoSorter.Core.ViewModels;

namespace PhotoSorter.App.Views.Dialogs;

public partial class MessageDialogWindow : Window
{
    public MessageDialogWindow()
    {
        InitializeComponent();
    }

    public MessageDialogWindow(MessageDialogViewModel viewModel) : this()
    {
        DataContext = viewModel;
        viewModel.RequestClose += (_, _) => Close();
    }
}
