using Microsoft.Extensions.DependencyInjection;
using PhotoSorter.App.Services;
using PhotoSorter.Core.Services.Abstractions;

namespace PhotoSorter.App.DependencyInjection;

/// <summary>
/// Registers PhotoSorter.App's platform/UI-specific implementations of Core-declared
/// abstractions (dialogs, dispatcher). Called alongside <c>AddPhotoSorterCore</c> in <see cref="Program"/>.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPhotoSorterApp(this IServiceCollection services)
    {
        services.AddSingleton<IFolderPickerService, AvaloniaFolderPickerService>();
        services.AddSingleton<IUiDispatcher, AvaloniaUiDispatcher>();
        services.AddSingleton<IApplyConfirmationDialogService, AvaloniaApplyConfirmationDialogService>();
        services.AddSingleton<IMessageDialogService, AvaloniaMessageDialogService>();
        services.AddSingleton<ISettingsDialogService, AvaloniaSettingsDialogService>();

        return services;
    }
}
