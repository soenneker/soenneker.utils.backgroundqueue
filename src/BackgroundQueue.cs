using System;
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
using Soenneker.Utils.BackgroundQueue.Dtos;
using Soenneker.Utils.Delay;

namespace Soenneker.Utils.BackgroundQueue;

/// <inheritdoc cref="IBackgroundQueue"/>
public sealed class BackgroundQueue : IBackgroundQueue
{
    private readonly Channel<ValueTaskEnvelope> _valueTaskChannel;
    private readonly Channel<TaskEnvelope> _taskChannel;

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

        _valueTaskChannel = Channel.CreateBounded<ValueTaskEnvelope>(options);
        _taskChannel = Channel.CreateBounded<TaskEnvelope>(options);
    }

    public async ValueTask QueueValueTask(Func<CancellationToken, ValueTask> workItem, CancellationToken cancellationToken = default)
    {
        int count = await _queueInformationUtil.IncrementValueTaskCounter(cancellationToken)
                                               .NoSync();

        try
        {
            // Store the user delegate as state; invoke via a static callback (no closure here)
            var env = new ValueTaskEnvelope(static (s, ct) => ((Func<CancellationToken, ValueTask>)s!).Invoke(ct), workItem);

            await _valueTaskChannel.Writer.WriteAsync(env, cancellationToken)
                                   .NoSync();
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

        if (_log)
            _logger.LogDebug("Queuing ValueTask: {name}", workItem.Method.GetSignature());
    }

    public async ValueTask QueueTask(Func<CancellationToken, Task> workItem, CancellationToken cancellationToken = default)
    {
        int count = await _queueInformationUtil.IncrementTaskCounter(cancellationToken)
                                               .NoSync();

        try
        {
            var env = new TaskEnvelope(static (s, ct) => ((Func<CancellationToken, Task>)s!).Invoke(ct), workItem);

            await _taskChannel.Writer.WriteAsync(env, cancellationToken)
                              .NoSync();
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

        if (_log)
            _logger.LogDebug("Queuing Task: {name}", workItem.Method.GetSignature());
    }

    public async ValueTask QueueValueTask<TState>(TState state, ValueTaskWorkItem<TState> workItem, CancellationToken cancellationToken = default)
    {
        int count = await _queueInformationUtil.IncrementValueTaskCounter(cancellationToken)
                                               .NoSync();

        try
        {
            // Pack BOTH delegate + state into the envelope state
            var env = new ValueTaskEnvelope(static (s, ct) =>
            {
                var payload = ((ValueTaskWorkItem<TState> work, TState st))s!;
                return payload.work(payload.st, ct);
            }, (workItem, state));

            await _valueTaskChannel.Writer.WriteAsync(env, cancellationToken)
                                   .NoSync();
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

        if (_log)
            _logger.LogDebug("Queuing ValueTask: {name}", workItem.Method.GetSignature());
    }

    public async ValueTask QueueTask<TState>(TState state, TaskWorkItem<TState> workItem, CancellationToken cancellationToken = default)
    {
        int count = await _queueInformationUtil.IncrementTaskCounter(cancellationToken)
                                               .NoSync();

        try
        {
            var env = new TaskEnvelope(static (s, ct) =>
            {
                var payload = ((TaskWorkItem<TState> work, TState st))s!;
                return payload.work(payload.st, ct);
            }, (workItem, state));

            await _taskChannel.Writer.WriteAsync(env, cancellationToken)
                              .NoSync();
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

        if (_log)
            _logger.LogDebug("Queuing Task: {name}", workItem.Method.GetSignature());
    }

    public ValueTask<ValueTaskEnvelope> DequeueValueTask(CancellationToken cancellationToken = default) =>
        _valueTaskChannel.Reader.ReadAsync(cancellationToken);

    public ValueTask<TaskEnvelope> DequeueTask(CancellationToken cancellationToken = default) => _taskChannel.Reader.ReadAsync(cancellationToken);

    public async ValueTask WaitUntilEmpty(CancellationToken cancellationToken = default)
    {
        const int delayMs = 500;

        bool isProcessing;

        do
        {
            isProcessing = await _queueInformationUtil.IsProcessing(cancellationToken)
                                                      .NoSync();

            if (isProcessing)
            {
                if (_log)
                    _logger.LogDebug("Delaying for {ms}ms (Background queue emptying)...", delayMs);

                await DelayUtil.Delay(delayMs, null, cancellationToken)
                               .NoSync();
            }
            else
            {
                _logger.LogDebug("Background queue is empty; continuing");
            }
        }
        while (isProcessing);
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