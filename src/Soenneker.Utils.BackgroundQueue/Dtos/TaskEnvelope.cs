using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Utils.BackgroundQueue.Dtos;

/// <summary>
/// Encapsulates a callback delegate and its associated state for deferred or asynchronous task execution.
/// </summary>
/// <remarks>A TaskEnvelope stores a callback function along with an optional state object, allowing the callback
/// to be invoked later with the provided state and a cancellation token. This struct is typically used to queue or
/// schedule work items that require both a delegate and contextual state. TaskEnvelope is immutable and
/// thread-safe.</remarks>
public readonly struct TaskEnvelope
{
    /// <summary>
    /// The state.
    /// </summary>
    public readonly object? State;

    /// <summary>
    /// The callback.
    /// </summary>
    public readonly Func<object?, CancellationToken, Task> Callback;

    public TaskEnvelope(Func<object?, CancellationToken, Task> callback, object? state)
    {
        Callback = callback;
        State = state;
    }

    /// <summary>
    /// Invokes the queued callback with its captured state and the supplied cancellation token.
    /// </summary>
    /// <param name="ct">Signals that the queued callback should stop.</param>
    /// <returns>An awaitable that completes when the callback finishes.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Task Invoke(CancellationToken ct) => Callback(State, ct);
}