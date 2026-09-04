using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Soenneker.Extensions.Double;
using Soenneker.Extensions.MethodInfo;
using Soenneker.Extensions.ValueTask;
using Soenneker.Utils.BackgroundQueue.Abstract;
using Soenneker.Utils.BackgroundQueue.Dtos;

namespace Soenneker.Utils.BackgroundQueue;

/// <inheritdoc cref="IBackgroundQueue"/>
public sealed class BackgroundQueue : IBackgroundQueue
{
    private readonly Channel<WorkItemEnvelope> _channel;

    private readonly int _queueLimit;
    private readonly int _queueWarning;

    private readonly ILogger<BackgroundQueue> _logger;
    private readonly IQueueInformationUtil _queueInformationUtil;

    private long _lastWarnTicks;

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
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false
        };

        _channel = Channel.CreateBounded<WorkItemEnvelope>(options);
    }

    public async ValueTask QueueValueTask(Func<CancellationToken, ValueTask> workItem, CancellationToken cancellationToken = default)
    {
        int count = await _queueInformationUtil.IncrementValueTaskCounter(cancellationToken)
                                               .NoSync();

        try
        {
            var env = new WorkItemEnvelope(static (work, _, ct) => ((Func<CancellationToken, ValueTask>)work!).Invoke(ct), workItem, null,
                isTask: false);

            await _channel.Writer.WriteAsync(env, cancellationToken).NoSync();
        }
        catch
        {
            await _queueInformationUtil.DecrementValueTaskCounter(CancellationToken.None)
                                       .NoSync();
            throw;
        }

        if (count > _queueWarning && ShouldWarn())
        {
            _logger.LogWarning(
                "ValueTask queue length ({length}) is currently greater than the warning ({warning}), and will wait after hitting limit ({limit})", count,
                _queueWarning, _queueLimit);
        }

        if (_log && _logger.IsEnabled(LogLevel.Debug))
            _logger.LogDebug("Queuing ValueTask: {name}", workItem.Method.GetSignature());
    }

    public async ValueTask QueueTask(Func<CancellationToken, Task> workItem, CancellationToken cancellationToken = default)
    {
        int count = await _queueInformationUtil.IncrementTaskCounter(cancellationToken)
                                               .NoSync();

        try
        {
            var env = new WorkItemEnvelope(static (work, _, ct) => new ValueTask(((Func<CancellationToken, Task>)work!).Invoke(ct)), workItem, null,
                isTask: true);

            await _channel.Writer.WriteAsync(env, cancellationToken).NoSync();
        }
        catch
        {
            await _queueInformationUtil.DecrementTaskCounter(CancellationToken.None)
                                       .NoSync();
            throw;
        }

        if (count > _queueWarning && ShouldWarn())
        {
            _logger.LogWarning("Task queue length ({length}) is currently greater than the warning ({warning}), and will wait after hitting limit ({limit})",
                count, _queueWarning, _queueLimit);
        }

        if (_log && _logger.IsEnabled(LogLevel.Debug))
            _logger.LogDebug("Queuing Task: {name}", workItem.Method.GetSignature());
    }

    public async ValueTask QueueValueTask<TState>(TState state, ValueTaskWorkItem<TState> workItem, CancellationToken cancellationToken = default)
    {
        int count = await _queueInformationUtil.IncrementValueTaskCounter(cancellationToken)
                                               .NoSync();

        try
        {
            var env = new WorkItemEnvelope(static (work, queuedState, ct) =>
                ((ValueTaskWorkItem<TState>)work!).Invoke((TState)queuedState!, ct), workItem, state, isTask: false);

            await _channel.Writer.WriteAsync(env, cancellationToken).NoSync();
        }
        catch
        {
            await _queueInformationUtil.DecrementValueTaskCounter(CancellationToken.None)
                                       .NoSync();
            throw;
        }

        if (count > _queueWarning && ShouldWarn())
        {
            _logger.LogWarning(
                "ValueTask queue length ({length}) is currently greater than the warning ({warning}), and will wait after hitting limit ({limit})", count,
                _queueWarning, _queueLimit);
        }

        if (_log && _logger.IsEnabled(LogLevel.Debug))
            _logger.LogDebug("Queuing ValueTask: {name}", workItem.Method.GetSignature());
    }

    public async ValueTask QueueTask<TState>(TState state, TaskWorkItem<TState> workItem, CancellationToken cancellationToken = default)
    {
        int count = await _queueInformationUtil.IncrementTaskCounter(cancellationToken)
                                               .NoSync();

        try
        {
            var env = new WorkItemEnvelope(static (work, queuedState, ct) =>
                new ValueTask(((TaskWorkItem<TState>)work!).Invoke((TState)queuedState!, ct)), workItem, state, isTask: true);

            await _channel.Writer.WriteAsync(env, cancellationToken).NoSync();
        }
        catch
        {
            await _queueInformationUtil.DecrementTaskCounter(CancellationToken.None)
                                       .NoSync();
            throw;
        }

        if (count > _queueWarning && ShouldWarn())
        {
            _logger.LogWarning("Task queue length ({length}) is currently greater than the warning ({warning}), and will wait after hitting limit ({limit})",
                count, _queueWarning, _queueLimit);
        }

        if (_log && _logger.IsEnabled(LogLevel.Debug))
            _logger.LogDebug("Queuing Task: {name}", workItem.Method.GetSignature());
    }

    public ValueTask<WorkItemEnvelope> Dequeue(CancellationToken cancellationToken = default) => _channel.Reader.ReadAsync(cancellationToken);

    public ValueTask WaitUntilEmpty(CancellationToken cancellationToken = default)
    {
        if (_log)
            _logger.LogDebug("Waiting for the background queue to empty...");

        return _queueInformationUtil.WaitUntilEmpty(cancellationToken);
    }

    private bool ShouldWarn()
    {
        long now = Environment.TickCount64;
        long last = Volatile.Read(ref _lastWarnTicks);

        if (now - last < 10_000) // 10s
            return false;

        return Interlocked.CompareExchange(ref _lastWarnTicks, now, last) == last;
    }
}
