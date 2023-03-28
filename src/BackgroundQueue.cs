using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
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

    public BackgroundQueue(IConfiguration config, ILogger<BackgroundQueue> logger)
    {
        _logger = logger;

        var configQueueLength = config.GetValue<int>("Background:QueueLength");
        _log = config.GetValue<bool>("Background:Log");

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

    public ValueTask QueueValueTask(Func<CancellationToken, ValueTask> workItem)
    {
        // TODO: need to redo this, we're going to get too many warnings

        int valueTaskCount = _valueTaskChannel.Reader.Count;

        if (valueTaskCount > _queueWarning)
            _logger.LogWarning("ValueTask queue length ({length}) is currently greater than the warning ({_queueWarning}), and will wait after hitting limit ({_queueLimit})", valueTaskCount, _queueWarning, _queueLimit);

        if (_log)
            _logger.LogDebug("Queuing ValueTask: {name}", workItem.ToString());

        ValueTask result = _valueTaskChannel.Writer.WriteAsync(workItem);
        return result;
    }

    public ValueTask QueueTask(Func<CancellationToken, Task> workItem)
    {
        int taskCount = _taskChannel.Reader.Count;

        if (taskCount > _queueWarning)
            _logger.LogWarning("ValueTask queue length ({length}) is currently greater than the warning ({_queueWarning}), and will wait after hitting limit ({_queueLimit})", taskCount, _queueWarning, _queueLimit);

        if (_log)
            _logger.LogDebug("Queuing Task: {name}", workItem.Method.GetSignature());

        ValueTask result = _taskChannel.Writer.WriteAsync(workItem);
        return result;
    }

    public ValueTask<Func<CancellationToken, ValueTask>> DequeueValueTask(CancellationToken cancellationToken)
    {
        return _valueTaskChannel.Reader.ReadAsync(cancellationToken);
    }

    public ValueTask<Func<CancellationToken, Task>> DequeueTask(CancellationToken cancellationToken)
    {
        return _taskChannel.Reader.ReadAsync(cancellationToken);
    }

    public (int TaskLength, int ValueTaskLength) GetLengthsOfQueues()
    {
        int valueTaskLength = _valueTaskChannel.Reader.Count;

        int taskLength = _taskChannel.Reader.Count;

        return (taskLength, valueTaskLength);
    }
}