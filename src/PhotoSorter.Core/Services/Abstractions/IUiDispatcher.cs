using System;

namespace PhotoSorter.Core.Services.Abstractions;

/// <summary>
/// Marshals an action onto the UI thread. ViewModels use this when reacting to events raised
/// by Core services, which know nothing about UI threads and may raise from a background thread.
/// Abstracted (rather than calling Avalonia's Dispatcher directly) so Core stays UI-framework-free
/// and ViewModel reaction logic is unit-testable with a synchronous fake.
/// </summary>
public interface IUiDispatcher
{
    void Post(Action action);
}
