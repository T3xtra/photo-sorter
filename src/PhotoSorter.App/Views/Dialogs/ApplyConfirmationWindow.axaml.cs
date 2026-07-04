using Avalonia.Controls;
using PhotoSorter.Core.ViewModels;

namespace PhotoSorter.App.Views.Dialogs;

public partial class ApplyConfirmationWindow : Window
{
    public ApplyConfirmationWindow()
    {
        InitializeComponent();
    }

    public ApplyConfirmationWindow(ApplyConfirmationViewModel viewModel) : this()
    {
        DataContext = viewModel;
        viewModel.RequestClose += (_, _) => Close(viewModel.Result);
    }
}
