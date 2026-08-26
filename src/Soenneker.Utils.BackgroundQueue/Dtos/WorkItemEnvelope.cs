using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Utils.BackgroundQueue.Dtos;

/// <summary>
/// Represents either Task- or ValueTask-based work in the shared background queue.
/// The delegate and state are stored separately so reference-type state does not require a boxed tuple.
/// </summary>
public readonly struct WorkItemEnvelope
{
    private readonly Func<object?, object?, CancellationToken, ValueTask> _callback;

    public readonly object? WorkItem;
    public readonly object? State;
    public readonly bool IsTask;

    internal WorkItemEnvelope(Func<object?, object?, CancellationToken, ValueTask> callback, object workItem, object? state, bool isTask)
    {
        _callback = callback;
        WorkItem = workItem;
        State = state;
        IsTask = isTask;
    }

    public MethodInfo? Method => (WorkItem as Delegate)?.Method;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask Invoke(CancellationToken cancellationToken) => _callback(WorkItem, State, cancellationToken);
}
