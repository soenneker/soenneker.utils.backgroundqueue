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
    private ValueAtomicInt _totalCount;
    private TaskCompletionSource _emptySignal = CreateCompletedSignal();

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
        return ValueTask.FromResult(_totalCount.Value > 0);
    }

    public ValueTask WaitUntilEmpty(CancellationToken cancellationToken = default)
    {
        if (_totalCount.Value == 0)
            return ValueTask.CompletedTask;

        return WaitUntilEmptySlow(cancellationToken);
    }

    public ValueTask<int> IncrementValueTaskCounter(CancellationToken cancellationToken = default)
    {
        int count = _valueTaskCount.Increment();
        MarkQueued();
        return ValueTask.FromResult(_trackCounts ? count : 0);
    }

    public ValueTask<int> DecrementValueTaskCounter(CancellationToken cancellationToken = default)
    {
        TaskCompletionSource signal = Volatile.Read(ref _emptySignal);
        int count = _valueTaskCount.Decrement();
        MarkCompleted(signal);
        return ValueTask.FromResult(_trackCounts ? count : 0);
    }

    public ValueTask<int> IncrementTaskCounter(CancellationToken cancellationToken = default)
    {
        int count = _taskCount.Increment();
        MarkQueued();
        return ValueTask.FromResult(_trackCounts ? count : 0);
    }

    public ValueTask<int> DecrementTaskCounter(CancellationToken cancellationToken = default)
    {
        TaskCompletionSource signal = Volatile.Read(ref _emptySignal);
        int count = _taskCount.Decrement();
        MarkCompleted(signal);
        return ValueTask.FromResult(_trackCounts ? count : 0);
    }

    private void MarkQueued()
    {
        if (_totalCount.Increment() == 1)
            Volatile.Write(ref _emptySignal, new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously));
    }

    private void MarkCompleted(TaskCompletionSource signal)
    {
        if (_totalCount.Decrement() == 0)
            signal.TrySetResult();
    }

    private async ValueTask WaitUntilEmptySlow(CancellationToken cancellationToken)
    {
        while (_totalCount.Value != 0)
        {
            TaskCompletionSource signal = Volatile.Read(ref _emptySignal);
            if (_totalCount.Value == 0)
                return;

            await signal.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private static TaskCompletionSource CreateCompletedSignal()
    {
        var signal = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        signal.SetResult();
        return signal;
    }
}
