using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Utils.BackgroundQueue.Dtos;

/// <summary>
/// Encapsulates a callback delegate and an associated state object for deferred asynchronous execution using a
/// ValueTask.
/// </summary>
/// <remarks>ValueTaskEnvelope is typically used to package a callback and its state for later invocation,
/// enabling efficient asynchronous operations without additional allocations. The callback is expected to accept the
/// state object and a CancellationToken, returning a ValueTask to represent the asynchronous operation. This struct is
/// immutable and thread-safe for concurrent use.</remarks>
public readonly struct ValueTaskEnvelope
{
    /// <summary>
    /// The state.
    /// </summary>
    public readonly object? State;

    /// <summary>
    /// The callback.
    /// </summary>
    public readonly Func<object?, CancellationToken, ValueTask> Callback;

    public ValueTaskEnvelope(Func<object?, CancellationToken, ValueTask> callback, object? state)
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
    public ValueTask Invoke(CancellationToken ct) => Callback(State, ct);
}