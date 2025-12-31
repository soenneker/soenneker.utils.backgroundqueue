using System;
using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Utils.BackgroundQueue.Dtos;

public readonly struct TaskEnvelope
{
    public readonly object? State;

    public readonly Func<object?, CancellationToken, Task> Callback;

    public TaskEnvelope(Func<object?, CancellationToken, Task> callback, object? state)
    {
        Callback = callback;
        State = state;
    }

    public Task Invoke(CancellationToken ct) => Callback(State, ct);
}