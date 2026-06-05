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
    /// Executes the invoke operation.
    /// </summary>
    /// <param name="ct">The ct.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask Invoke(CancellationToken ct) => Callback(State, ct);
}