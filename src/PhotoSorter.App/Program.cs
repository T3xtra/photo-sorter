using System;
using System.IO;
using Avalonia;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PhotoSorter.App.DependencyInjection;
using PhotoSorter.Core.DependencyInjection;
using PhotoSorter.Core.Services.Abstractions;
using PhotoSorter.Core.Services.Implementations;
using Serilog;

namespace PhotoSorter.App;

internal sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        var services = BuildServiceProvider();
        App.Services = services;

        try
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();

    private static ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();

        // The path provider is created eagerly here (outside the container) because the log
        // directory must exist before Serilog is configured. Registering the instance before
        // AddPhotoSorterCore() means Core's TryAddSingleton for the same interface is a no-op.
        var pathProvider = new AppPathProvider();
        services.AddSingleton<IAppPathProvider>(pathProvider);
        services.AddPhotoSorterCore();
        services.AddPhotoSorterApp();

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .WriteTo.File(
                Path.Combine(pathProvider.LogDirectory, "photosorter-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14)
            .CreateLogger();

        services.AddLogging(builder => builder.AddSerilog(dispose: true));

        return services.BuildServiceProvider();
    }
}
