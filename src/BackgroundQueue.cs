using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nito.AsyncEx;
using Soenneker.Extensions.Double;
using Soenneker.Extensions.MethodInfo;
using Soenneker.Utils.BackgroundQueue.Abstract;

namespace Soenneker.Utils.BackgroundQueue;

/// <inheritdoc cref="IBackgroundQueue"/>
public class BackgroundQueue : IBackgroundQueue
{
    private readonly Channel<Func<CancellationToken, ValueTask>> _valueTaskChannel;
    private readonly Channel<Func<CancellationToken, Task>> _taskChannel;

    private readonly int _queueLimit;
    private readonly int _queueWarning;

    private readonly ILogger<BackgroundQueue> _logger;

    private readonly bool _log;
    private readonly bool _lockCounts;

    private readonly AsyncLock? _asyncLock;

    private int _taskChannelCount;
    private int _valueTaskChannelCount;

    public BackgroundQueue(IConfiguration config, ILogger<BackgroundQueue> logger)
    {
        _logger = logger;

        var configQueueLength = config.GetValue<int>("Background:QueueLength");
        _log = config.GetValue<bool>("Background:Log");
        _lockCounts = config.GetValue<bool>("Background:LockCounts");

        if (_lockCounts)
            _asyncLock = new AsyncLock();

        if (configQueueLength > 1)
        {
            _queueLimit = configQueueLength;
        }
        else
        {
            _queueLimit = 5000;
            _logger.LogError("Background queue limit was not set or invalid in config, setting from default to: {length}. Fix!", _queueLimit);
        }

        _queueWarning = (_queueLimit * .5).ToInt();

        _logger.LogDebug("Creating background queue with limit: {length}", _queueLimit);

        var options = new BoundedChannelOptions(_queueLimit)
        {
            FullMode = BoundedChannelFullMode.Wait
        };

        _valueTaskChannel = Channel.CreateBounded<Func<CancellationToken, ValueTask>>(options);
        _taskChannel = Channel.CreateBounded<Func<CancellationToken, Task>>(options);
    }

    public async ValueTask QueueValueTask(Func<CancellationToken, ValueTask> workItem)
    {
        // TODO: need to redo this, we're going to get too many warnings

        await ChangeValueTaskCounter(true);

        if (_valueTaskChannelCount > _queueWarning)
            _logger.LogWarning("ValueTask queue length ({length}) is currently greater than the warning ({_queueWarning}), and will wait after hitting limit ({_queueLimit})", _valueTaskChannelCount, _queueWarning, _queueLimit);

        if (_log)
            _logger.LogDebug("Queuing ValueTask: {name}", workItem.ToString());

        await _valueTaskChannel.Writer.WriteAsync(workItem);
    }

    public async ValueTask QueueTask(Func<CancellationToken, Task> workItem)
    {
        await ChangeTaskCounter(true);

        if (_taskChannelCount > _queueWarning)
            _logger.LogWarning("ValueTask queue length ({length}) is currently greater than the warning ({_queueWarning}), and will wait after hitting limit ({_queueLimit})", _taskChannelCount, _queueWarning, _queueLimit);

        if (_log)
            _logger.LogDebug("Queuing Task: {name}", workItem.Method.GetSignature());

        await _taskChannel.Writer.WriteAsync(workItem);
    }

    private async ValueTask ChangeValueTaskCounter(bool increment)
    {
        if (!_lockCounts)
        {
            if (increment)
                Interlocked.Increment(ref _valueTaskChannelCount);
            else
                Interlocked.Decrement(ref _valueTaskChannelCount);
            return;
        }

        using (await _asyncLock!.LockAsync())
        {
            if (increment)
                _valueTaskChannelCount++;
            else
                _valueTaskChannelCount--;
        }
    }

    private async ValueTask ChangeTaskCounter(bool increment)
    {
        if (!_lockCounts)
        {
            if (increment)
                Interlocked.Increment(ref _taskChannelCount);
            else
                Interlocked.Decrement(ref _taskChannelCount);
            return;
        }

        using (await _asyncLock!.LockAsync())
        {
            if (increment)
                _taskChannelCount++;
            else
                _taskChannelCount--;
        }
    }

    public async ValueTask<Func<CancellationToken, ValueTask>> DequeueValueTask(CancellationToken cancellationToken)
    {
        Func<CancellationToken, ValueTask> result = await _valueTaskChannel.Reader.ReadAsync(cancellationToken);

        await ChangeValueTaskCounter(false);

        return result;
    }

    public async ValueTask<Func<CancellationToken, Task>> DequeueTask(CancellationToken cancellationToken)
    {
        Func<CancellationToken, Task> result = await _taskChannel.Reader.ReadAsync(cancellationToken);

        await ChangeTaskCounter(false);

        return result;
    }

    public async ValueTask<(int TaskLength, int ValueTaskLength)> GetCountsOfChannels()
    {
        if (!_lockCounts)
            return (_taskChannelCount, _valueTaskChannelCount);

        using (await _asyncLock!.LockAsync())
        {
            return (_taskChannelCount, _valueTaskChannelCount);
        }
    }
}