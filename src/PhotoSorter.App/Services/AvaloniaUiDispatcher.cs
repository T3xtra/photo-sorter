using System;
using Avalonia.Threading;
using PhotoSorter.Core.Services.Abstractions;

namespace PhotoSorter.App.Services;

/// <inheritdoc cref="IUiDispatcher"/>
public sealed class AvaloniaUiDispatcher : IUiDispatcher
{
    public void Post(Action action) => Dispatcher.UIThread.Post(action);
}
