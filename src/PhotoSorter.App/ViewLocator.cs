using Avalonia.Controls;
using Avalonia.Controls.Templates;
using PhotoSorter.App.Views;
using PhotoSorter.Core.ViewModels;

namespace PhotoSorter.App;

/// <summary>
/// Maps ViewModels to their Views explicitly. ViewModels live in PhotoSorter.Core while Views
/// live in PhotoSorter.App, so the two namespaces don't mirror each other and a naming-convention
/// based locator (reflection over "ViewModel" -&gt; "View" name replacement) cannot resolve them.
/// An explicit map also avoids reflection, keeping the app trimming/AOT friendly.
/// </summary>
public sealed class ViewLocator : IDataTemplate
{
    public Control? Build(object? param) => param switch
    {
        MainWindowViewModel => new MainWindow(),
        _ => new TextBlock { Text = $"No view registered for {param?.GetType().FullName}" },
    };

    public bool Match(object? data) => data is ViewModelBase;
}
