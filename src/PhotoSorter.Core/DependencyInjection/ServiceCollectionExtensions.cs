using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PhotoSorter.Core.Services.Abstractions;
using PhotoSorter.Core.Services.Implementations;
using PhotoSorter.Core.ViewModels;

namespace PhotoSorter.Core.DependencyInjection;

/// <summary>
/// Registers PhotoSorter.Core services and ViewModels with a dependency injection container.
/// Keeping registration in Core (rather than the App composition root) means any host
/// (the Avalonia app, a future CLI, or tests) can pull in the same wiring.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers Core services and ViewModels. Uses <c>TryAdd</c> so a host that needs to
    /// construct a service earlier (e.g. the App project needs <see cref="IAppPathProvider"/>
    /// before the container exists, to configure logging) can register its own instance first
    /// without ending up with two competing registrations.
    /// </summary>
    public static IServiceCollection AddPhotoSorterCore(this IServiceCollection services)
    {
        services.TryAddSingleton<IAppPathProvider, AppPathProvider>();
        services.TryAddSingleton<ISettingsService, JsonSettingsService>();
        services.TryAddSingleton<IFolderScannerService, FolderScannerService>();
        services.TryAddSingleton<IProjectService, ProjectService>();
        services.TryAddSingleton<ImageSharpImageDecoder>();
        services.TryAddSingleton<RawPreviewImageDecoder>();
        services.TryAddSingleton<IImageDecoder, CompositeImageDecoder>();
        services.TryAddSingleton<ImageSharpThumbnailGenerator>();
        services.TryAddSingleton<RawThumbnailGenerator>();
        services.TryAddSingleton<IThumbnailGenerator, CompositeThumbnailGenerator>();
        services.TryAddSingleton<IHotkeyService, HotkeyService>();
        services.TryAddSingleton<IProjectFileSerializer, JsonProjectFileSerializer>();
        services.TryAddSingleton<ITrashService, PlatformTrashService>();
        services.TryAddSingleton<IFileMoveService, FileMoveService>();
        services.TryAddSingleton<IImageCache, ImageCache>();

        // IFolderPickerService, IUiDispatcher, IApplyConfirmationDialogService and
        // IMessageDialogService are platform/UI-specific and registered by the host (see
        // PhotoSorter.App's AddPhotoSorterApp) - Core only declares the abstractions.

        // Singletons: there is exactly one instance of each for the lifetime of the app,
        // matching the single main window they belong to.
        services.AddSingleton<ToolbarViewModel>();
        services.AddSingleton<ThumbnailBarViewModel>();
        services.AddSingleton<ImageViewerViewModel>();
        services.AddSingleton<StatusBarViewModel>();
        services.AddSingleton<MainWindowViewModel>();

        return services;
    }
}
