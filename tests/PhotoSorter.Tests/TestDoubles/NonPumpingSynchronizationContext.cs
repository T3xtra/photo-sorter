using System.Threading;

namespace PhotoSorter.Tests.TestDoubles;

/// <summary>
/// A <see cref="SynchronizationContext"/> that captures posted continuations but never runs
/// them, simulating a UI dispatcher thread that is itself busy (blocked on a synchronous wait).
/// If library code correctly uses <c>ConfigureAwait(false)</c>, it never needs this context to
/// resume and completes regardless. If it doesn't, the awaited continuation is posted here and
/// silently never runs, so the awaiting call hangs forever.
/// </summary>
public sealed class NonPumpingSynchronizationContext : SynchronizationContext
{
    public override void Post(SendOrPostCallback d, object? state)
    {
        // Deliberately dropped.
    }

    public override void Send(SendOrPostCallback d, object? state)
    {
        // Deliberately dropped.
    }
}
