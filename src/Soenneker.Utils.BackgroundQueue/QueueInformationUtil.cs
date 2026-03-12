using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Soenneker.Atomics.ValueInts;
using Soenneker.Utils.BackgroundQueue.Abstract;

namespace Soenneker.Utils.BackgroundQueue;

/// <inheritdoc cref="IQueueInformationUtil"/>
public sealed class QueueInformationUtil : IQueueInformationUtil
{
    private readonly bool _trackCounts;

    private ValueAtomicInt _taskCount;
    private ValueAtomicInt _valueTaskCount;

    public QueueInformationUtil(IConfiguration config)
    {
        _trackCounts = config.GetValue<bool>("Background:LockCounts");
    }

    public ValueTask<(int TaskLength, int ValueTaskLength)> GetCountsOfProcessing(CancellationToken cancellationToken = default)
    {
        if (!_trackCounts)
            return ValueTask.FromResult((0, 0));

        return ValueTask.FromResult((_taskCount.Value, _valueTaskCount.Value));
    }

    public ValueTask<bool> IsProcessing(CancellationToken cancellationToken = default)
    {
        if (!_trackCounts)
            return ValueTask.FromResult(false);

        return ValueTask.FromResult(_taskCount.Value > 0 || _valueTaskCount.Value > 0);
    }

    public ValueTask<int> IncrementValueTaskCounter(CancellationToken cancellationToken = default)
    {
        if (!_trackCounts)
            return ValueTask.FromResult(0);

        return ValueTask.FromResult(_valueTaskCount.Increment());
    }

    public ValueTask<int> DecrementValueTaskCounter(CancellationToken cancellationToken = default)
    {
        if (!_trackCounts)
            return ValueTask.FromResult(0);

        return ValueTask.FromResult(_valueTaskCount.Decrement());
    }

    public ValueTask<int> IncrementTaskCounter(CancellationToken cancellationToken = default)
    {
        if (!_trackCounts)
            return ValueTask.FromResult(0);

        return ValueTask.FromResult(_taskCount.Increment());
    }

    public ValueTask<int> DecrementTaskCounter(CancellationToken cancellationToken = default)
    {
        if (!_trackCounts)
            return ValueTask.FromResult(0);

        return ValueTask.FromResult(_taskCount.Decrement());
    }
}