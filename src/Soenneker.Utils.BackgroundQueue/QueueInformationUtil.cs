using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Soenneker.Extensions.Task;
using Soenneker.Utils.BackgroundQueue.Abstract;

namespace Soenneker.Utils.BackgroundQueue;

public sealed class QueueInformationUtil : IQueueInformationUtil
{
    private readonly bool _trackCounts;
    private readonly Lock _gate = new();
    private int _taskCount;
    private int _valueTaskCount;
    private int _totalCount;
    private TaskCompletionSource? _emptySignal;

    public QueueInformationUtil(IConfiguration config) => _trackCounts = config.GetValue<bool>("Background:LockCounts");

    public ValueTask<(int TaskLength, int ValueTaskLength)> GetCountsOfProcessing(CancellationToken cancellationToken = default)
    {
        if (!_trackCounts)
            return ValueTask.FromResult((0, 0));

        lock (_gate)
            return ValueTask.FromResult((_taskCount, _valueTaskCount));
    }

    public ValueTask<bool> IsProcessing(CancellationToken cancellationToken = default) => ValueTask.FromResult(Volatile.Read(ref _totalCount) > 0);

    public ValueTask WaitUntilEmpty(CancellationToken cancellationToken = default)
    {
        if (Volatile.Read(ref _totalCount) == 0)
            return ValueTask.CompletedTask;

        return WaitUntilEmptySlow(cancellationToken);
    }

    public ValueTask<int> IncrementValueTaskCounter(CancellationToken cancellationToken = default) => Increment(ref _valueTaskCount);
    public ValueTask<int> DecrementValueTaskCounter(CancellationToken cancellationToken = default) => Decrement(ref _valueTaskCount);
    public ValueTask<int> IncrementTaskCounter(CancellationToken cancellationToken = default) => Increment(ref _taskCount);
    public ValueTask<int> DecrementTaskCounter(CancellationToken cancellationToken = default) => Decrement(ref _taskCount);

    private ValueTask<int> Increment(ref int counter)
    {
        lock (_gate)
        {
            int count = ++counter;
            Volatile.Write(ref _totalCount, _totalCount + 1);
            return ValueTask.FromResult(_trackCounts ? count : 0);
        }
    }

    private ValueTask<int> Decrement(ref int counter)
    {
        TaskCompletionSource? signal = null;
        int count;
        lock (_gate)
        {
            count = --counter;
            Volatile.Write(ref _totalCount, _totalCount - 1);
            if (_totalCount == 0)
            {
                signal = _emptySignal;
                _emptySignal = null;
            }
        }

        signal?.TrySetResult();
        return ValueTask.FromResult(_trackCounts ? count : 0);
    }

    private async ValueTask WaitUntilEmptySlow(CancellationToken cancellationToken)
    {
        while (true)
        {
            Task task;
            lock (_gate)
            {
                if (_totalCount == 0)
                    return;

                // Allocate only for an actual waiter, and publish atomically with the count.
                _emptySignal ??= new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                task = _emptySignal.Task;
            }

            await task.WaitAsync(cancellationToken).NoSync();
        }
    }
}
