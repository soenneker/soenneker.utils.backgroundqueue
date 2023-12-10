using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Nito.AsyncEx;
using Soenneker.Utils.BackgroundQueue.Abstract;

namespace Soenneker.Utils.BackgroundQueue;

///<inheritdoc cref="IQueueInformationUtil"/>
public sealed class QueueInformationUtil : IQueueInformationUtil
{
    private readonly bool _lockCounts;

    private readonly AsyncLock? _asyncLock;

    private int _taskCount;
    private int _valueTaskCount;

    public QueueInformationUtil(IConfiguration config)
    {
        _lockCounts = config.GetValue<bool>("Background:LockCounts");

        if (_lockCounts)
            _asyncLock = new AsyncLock();
    }

    public async ValueTask<(int TaskLength, int ValueTaskLength)> GetCountsOfProcessing()
    {
        if (!_lockCounts)
            return (_taskCount, _valueTaskCount);

        using (await _asyncLock!.LockAsync().ConfigureAwait(false))
        {
            return (_taskCount, _valueTaskCount);
        }
    }

    public async ValueTask<bool> IsProcessing()
    {
        if (!_lockCounts)
        {
            if (_valueTaskCount > 0 || _taskCount > 0)
                return true;

            return false;
        }

        using (await _asyncLock!.LockAsync().ConfigureAwait(false))
        {
            if (_valueTaskCount > 0 || _taskCount > 0)
                return true;

            return false;
        }
    }

    public async ValueTask<int> IncrementValueTaskCounter()
    {
        if (!_lockCounts)
        {
            Interlocked.Increment(ref _valueTaskCount);

            return _valueTaskCount;
        }

        using (await _asyncLock!.LockAsync().ConfigureAwait(false))
        {
            _valueTaskCount++;

            return _valueTaskCount;
        }
    }

    public async ValueTask<int> DecrementValueTaskCounter()
    {
        if (!_lockCounts)
        {
            Interlocked.Decrement(ref _valueTaskCount);

            return _valueTaskCount;
        }

        using (await _asyncLock!.LockAsync().ConfigureAwait(false))
        {
            _valueTaskCount--;

            return _valueTaskCount;
        }
    }

    public async ValueTask<int> IncrementTaskCounter()
    {
        if (!_lockCounts)
        {
            Interlocked.Increment(ref _taskCount);

            return _taskCount;
        }

        using (await _asyncLock!.LockAsync().ConfigureAwait(false))
        {
            _taskCount++;

            return _taskCount;
        }
    }

    public async ValueTask<int> DecrementTaskCounter()
    {
        if (!_lockCounts)
        {
            Interlocked.Decrement(ref _taskCount);
            return _taskCount;
        }

        using (await _asyncLock!.LockAsync().ConfigureAwait(false))
        {
            _taskCount--;

            return _taskCount;
        }
    }
}