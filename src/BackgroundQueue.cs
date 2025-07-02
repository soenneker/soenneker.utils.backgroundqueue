﻿using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Soenneker.Extensions.Double;
using Soenneker.Extensions.MethodInfo;
using Soenneker.Extensions.Task;
using Soenneker.Extensions.ValueTask;
using Soenneker.Utils.BackgroundQueue.Abstract;
using Soenneker.Utils.Delay;

namespace Soenneker.Utils.BackgroundQueue;

/// <inheritdoc cref="IBackgroundQueue"/>
public sealed class BackgroundQueue : IBackgroundQueue
{
    private readonly Channel<Func<CancellationToken, ValueTask>> _valueTaskChannel;
    private readonly Channel<Func<CancellationToken, Task>> _taskChannel;

    private readonly int _queueLimit;
    private readonly int _queueWarning;

    private readonly ILogger<BackgroundQueue> _logger;
    private readonly IQueueInformationUtil _queueInformationUtil;

    private readonly bool _log;

    public BackgroundQueue(IConfiguration config, ILogger<BackgroundQueue> logger, IQueueInformationUtil queueInformationUtil)
    {
        _logger = logger;
        _queueInformationUtil = queueInformationUtil;

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

    public async ValueTask QueueValueTask(Func<CancellationToken, ValueTask> workItem, CancellationToken cancellationToken = default)
    {
        // TODO: need to redo this, we're going to get too many warnings

        int count = await _queueInformationUtil.IncrementValueTaskCounter(cancellationToken).NoSync();

        if (count > _queueWarning)
        {
            _logger.LogWarning("ValueTask queue length ({length}) is currently greater than the warning ({_queueWarning}), and will wait after hitting limit ({_queueLimit})", count,
                _queueWarning, _queueLimit);
        }

        if (_log)
            _logger.LogDebug("Queuing ValueTask: {name}", workItem.ToString());

        await _valueTaskChannel.Writer.WriteAsync(workItem, cancellationToken).NoSync();
    }

    public async ValueTask QueueTask(Func<CancellationToken, Task> workItem, CancellationToken cancellationToken = default)
    {
        int count = await _queueInformationUtil.IncrementTaskCounter(cancellationToken).NoSync();

        if (count > _queueWarning)
        {
            _logger.LogWarning("ValueTask queue length ({length}) is currently greater than the warning ({_queueWarning}), and will wait after hitting limit ({_queueLimit})", count,
                _queueWarning, _queueLimit);
        }

        if (_log)
            _logger.LogDebug("Queuing Task: {name}", workItem.Method.GetSignature());

        await _taskChannel.Writer.WriteAsync(workItem, cancellationToken).NoSync();
    }

    public ValueTask<Func<CancellationToken, ValueTask>> DequeueValueTask(CancellationToken cancellationToken = default)
    {
        return _valueTaskChannel.Reader.ReadAsync(cancellationToken);
    }

    public ValueTask<Func<CancellationToken, Task>> DequeueTask(CancellationToken cancellationToken = default)
    {
        return _taskChannel.Reader.ReadAsync(cancellationToken);
    }

    public async ValueTask WaitUntilEmpty(CancellationToken cancellationToken = default)
    {
        const int delayMs = 500;

        bool isProcessing;

        do
        {
            isProcessing = await _queueInformationUtil.IsProcessing(cancellationToken).ConfigureAwait(false);

            if (isProcessing)
            {
                if (_log)
                {
                    _logger.LogDebug("Delaying for {ms}ms (Background queue emptying)...", delayMs);
                }

                await DelayUtil.Delay(delayMs, null, cancellationToken).NoSync();
            }
            else
            {
                _logger.LogDebug("Background queue is empty; continuing");
            }
        } while (isProcessing);
    }
}