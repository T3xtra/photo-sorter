using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using PhotoSorter.Core.Services.Implementations;
using PhotoSorter.Tests.TestDoubles;

namespace PhotoSorter.Tests.Services;

public sealed class JsonSettingsServiceTests
{
    [Fact]
    public async Task LoadAsync_WhenNoFileExists_UsesDefaults()
    {
        using var paths = new TempAppPathProvider();
        var sut = new JsonSettingsService(paths, NullLogger<JsonSettingsService>.Instance);

        await sut.LoadAsync();

        Assert.Equal(1280, sut.Current.WindowWidth);
        Assert.Equal(800, sut.Current.WindowHeight);
    }

    [Fact]
    public async Task SaveAsync_ThenLoadAsync_RoundTripsChangedValues()
    {
        using var paths = new TempAppPathProvider();
        var sut = new JsonSettingsService(paths, NullLogger<JsonSettingsService>.Instance);
        await sut.LoadAsync();

        sut.Current.WindowWidth = 1600;
        sut.Current.WindowHeight = 900;
        await sut.SaveAsync();

        var reloaded = new JsonSettingsService(paths, NullLogger<JsonSettingsService>.Instance);
        await reloaded.LoadAsync();

        Assert.Equal(1600, reloaded.Current.WindowWidth);
        Assert.Equal(900, reloaded.Current.WindowHeight);
    }

    [Fact]
    public async Task LoadAsync_WhenFileIsCorrupt_FallsBackToDefaultsWithoutThrowing()
    {
        using var paths = new TempAppPathProvider();
        await File.WriteAllTextAsync(paths.SettingsFilePath, "{ not valid json");

        var sut = new JsonSettingsService(paths, NullLogger<JsonSettingsService>.Instance);
        await sut.LoadAsync();

        Assert.Equal(1280, sut.Current.WindowWidth);
    }

    /// <summary>
    /// Regression test (Roadmap Phase 16: Zugriffsfehler): the previous implementation called
    /// File.OpenRead outside its try block, so a permission-denied settings file would throw an
    /// unhandled UnauthorizedAccessException straight out of App.axaml.cs's synchronous startup
    /// call - preventing the app from ever showing a window.
    /// </summary>
    [Fact]
    public async Task LoadAsync_WhenFileIsUnreadable_FallsBackToDefaultsWithoutThrowing()
    {
        if (OperatingSystem.IsWindows())
        {
            return; // POSIX permission bits aren't a reliable way to force access-denied on Windows.
        }

        using var paths = new TempAppPathProvider();
        await File.WriteAllTextAsync(paths.SettingsFilePath, """{"WindowWidth":1600,"WindowHeight":900}""");
        File.SetUnixFileMode(paths.SettingsFilePath, UnixFileMode.None);

        var sut = new JsonSettingsService(paths, NullLogger<JsonSettingsService>.Instance);
        await sut.LoadAsync();

        Assert.Equal(1280, sut.Current.WindowWidth);
    }

    /// <summary>
    /// Regression test for a deadlock where App.axaml.cs calls
    /// <c>settingsService.LoadAsync().GetAwaiter().GetResult()</c> synchronously during Avalonia
    /// startup. Without ConfigureAwait(false) inside LoadAsync, the continuation after the
    /// internal await tries to resume on the calling thread's SynchronizationContext - the very
    /// thread that is blocked waiting for it - and never completes.
    /// </summary>
    [Fact]
#pragma warning disable xUnit1031 // Blocking is the behavior under test: it must not deadlock.
    public void LoadAsync_CalledSynchronouslyUnderUiLikeSynchronizationContext_DoesNotDeadlock()
    {
        using var paths = new TempAppPathProvider();
        File.WriteAllText(paths.SettingsFilePath, """{"WindowWidth":1600,"WindowHeight":900}""");

        var sut = new JsonSettingsService(paths, NullLogger<JsonSettingsService>.Instance);

        var completed = Task.Run(() =>
        {
            SynchronizationContext.SetSynchronizationContext(new NonPumpingSynchronizationContext());
            sut.LoadAsync().GetAwaiter().GetResult();
        }).Wait(TimeSpan.FromSeconds(5));

        Assert.True(completed, "LoadAsync deadlocked when awaited synchronously under a captured SynchronizationContext.");
        Assert.Equal(1600, sut.Current.WindowWidth);
    }
#pragma warning restore xUnit1031

    /// <summary>
    /// Same regression as above, for the SaveAsync call made synchronously from the
    /// desktop lifetime's ShutdownRequested handler in App.axaml.cs.
    /// </summary>
    [Fact]
#pragma warning disable xUnit1031 // Blocking is the behavior under test: it must not deadlock.
    public void SaveAsync_CalledSynchronouslyUnderUiLikeSynchronizationContext_DoesNotDeadlock()
    {
        using var paths = new TempAppPathProvider();
        var sut = new JsonSettingsService(paths, NullLogger<JsonSettingsService>.Instance);
        sut.Current.WindowWidth = 1920;

        var completed = Task.Run(() =>
        {
            SynchronizationContext.SetSynchronizationContext(new NonPumpingSynchronizationContext());
            sut.SaveAsync().GetAwaiter().GetResult();
        }).Wait(TimeSpan.FromSeconds(5));

        Assert.True(completed, "SaveAsync deadlocked when awaited synchronously under a captured SynchronizationContext.");
        Assert.Contains("1920", File.ReadAllText(paths.SettingsFilePath));
    }
#pragma warning restore xUnit1031
}
