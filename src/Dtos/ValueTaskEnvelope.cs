using System;
using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Utils.BackgroundQueue.Dtos;

public readonly struct ValueTaskEnvelope
{
    public readonly object? State;

    public readonly Func<object?, CancellationToken, ValueTask> Callback;

    public ValueTaskEnvelope(Func<object?, CancellationToken, ValueTask> callback, object? state)
    {
        Callback = callback;
        State = state;
    }

    public ValueTask Invoke(CancellationToken ct) => Callback(State, ct);
}