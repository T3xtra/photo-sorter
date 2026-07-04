using System;
using System.IO;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PhotoSorter.Core.Services.Abstractions;
using PhotoSorter.Core.ViewModels;
using PhotoSorter.App.Views;

namespace PhotoSorter.App;

public partial class App : Application
{
    /// <summary>
    /// The application's dependency injection container, built in <see cref="Program"/>
    /// before Avalonia's own initialization runs.
    /// </summary>
    public static IServiceProvider Services { get; set; } = null!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var settingsService = Services.GetRequiredService<ISettingsService>();
            var projectService = Services.GetRequiredService<IProjectService>();
            var pathProvider = Services.GetRequiredService<IAppPathProvider>();
            var logger = Services.GetRequiredService<ILogger<App>>();

            // Settings are loaded synchronously-over-async here because no window exists yet
            // to show a loading state, and the read is a small local file.
            settingsService.LoadAsync().GetAwaiter().GetResult();

            // "Nach Absturz kann die Sitzung vollständig wiederhergestellt werden" (SoftwareDesign.md).
            // Restored unconditionally (not just after a crash): resuming the last session is the
            // expected behavior for a sorting tool regardless of how the app was last closed - see
            // docs/architecture-decisions.md, Punkt 17.
            if (File.Exists(pathProvider.AutoSaveFilePath))
            {
                var restored = projectService.OpenAsync(pathProvider.AutoSaveFilePath).GetAwaiter().GetResult();
                logger.LogInformation("Session restore from {Path}: {Result}.", pathProvider.AutoSaveFilePath, restored ? "succeeded" : "failed");
            }

            logger.LogInformation("PhotoSorter starting up.");

            var mainWindow = new MainWindow
            {
                DataContext = Services.GetRequiredService<MainWindowViewModel>(),
                Width = settingsService.Current.WindowWidth,
                Height = settingsService.Current.WindowHeight,
            };

            desktop.MainWindow = mainWindow;

            desktop.ShutdownRequested += (_, _) =>
            {
                settingsService.Current.WindowWidth = (int)mainWindow.Width;
                settingsService.Current.WindowHeight = (int)mainWindow.Height;
                settingsService.SaveAsync().GetAwaiter().GetResult();
                logger.LogInformation("PhotoSorter shutting down.");
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
