using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nito.AsyncEx;
using Soenneker.Extensions.MethodInfo;
using Soenneker.Utils.BackgroundQueue.Abstract;

namespace Soenneker.Utils.BackgroundQueue;

/// <inheritdoc cref="IQueuedHostedService"/>
public class QueuedHostedService : BackgroundService, IQueuedHostedService
{
    private readonly IBackgroundQueue _queue;
    private readonly ILogger<QueuedHostedService> _logger;

    private readonly bool _log;
    private readonly bool _lockCounts;

    private readonly AsyncLock? _asyncLock;

    private int _taskProcessingCount;
    private int _valueTaskProcessingCount;

    public QueuedHostedService(IConfiguration config, IBackgroundQueue queue, ILogger<QueuedHostedService> logger)
    {
        _log = config.GetValue<bool>("Background:Log");
        _lockCounts = config.GetValue<bool>("Background:LockCounts");

        _queue = queue;
        _logger = logger;

        if (_lockCounts)
            _asyncLock = new AsyncLock();
    }

    /// <summary>
    /// Needs calling manually from unit test fixtures to start it
    /// </summary>
    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        if (_log)
            _logger.LogDebug("~~ QueuedHostedService: Starting...");

        await base.StartAsync(cancellationToken);
    }

    /// <summary>
    /// Triggered when the application host is ready to start the service.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_log)
            _logger.LogDebug("~~ QueuedHostedService: Executing...");

        Task valueTaskProcessing = ValueTaskProcessing(stoppingToken);
        Task taskProcessing = TaskProcessing(stoppingToken);

        await Task.WhenAll(valueTaskProcessing, taskProcessing);
    }

    private async Task TaskProcessing(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            Func<CancellationToken, Task> workItem = await _queue.DequeueTask(stoppingToken);

            string? workItemName = null;

            try
            {
                await ChangeTaskCounter(true);

                if (_log)
                {
                    workItemName = workItem.Method.GetSignature();
                    _logger.LogDebug("~~ QueuedHostedService: Starting Task: {item}", workItemName);
                }

                await workItem(stoppingToken);

                if (_log)
                    _logger.LogDebug("~~ QueuedHostedService: Completed Task: {item}", workItemName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "~~ QueuedHostedService:: Error executing Task: {item}.", workItem.Method.GetSignature());
            }
            finally
            {
                await ChangeTaskCounter(false);
            }
        }
    }

    private async ValueTask ChangeValueTaskCounter(bool increment)
    {
        if (!_lockCounts)
        {
            if (increment)
                Interlocked.Increment(ref _valueTaskProcessingCount);
            else
                Interlocked.Decrement(ref _valueTaskProcessingCount);
            return;
        }

        using (await _asyncLock!.LockAsync())
        {
            if (increment)
                _valueTaskProcessingCount++;
            else
                _valueTaskProcessingCount--;
        }
    }

    private async ValueTask ChangeTaskCounter(bool increment)
    {
        if (!_lockCounts)
        {
            if (increment)
                Interlocked.Increment(ref _taskProcessingCount);
            else
                Interlocked.Decrement(ref _taskProcessingCount);
            return;
        }

        using (await _asyncLock!.LockAsync())
        {
            if (increment)
                _taskProcessingCount++;
            else
                _taskProcessingCount--;
        }
    }
    
    private async Task ValueTaskProcessing(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            Func<CancellationToken, ValueTask> workItem = await _queue.DequeueValueTask(stoppingToken);

            string? workItemName = null;

            try
            {
                await ChangeValueTaskCounter(true);

                if (_log)
                {
                    workItemName = workItem.Method.GetSignature();
                    _logger.LogDebug("~~ QueuedHostedService: Starting ValueTask: {item}", workItemName);
                }

                await workItem(stoppingToken);

                if (_log)
                    _logger.LogDebug("~~ QueuedHostedService: Completed ValueTask: {item}", workItemName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "~~ QueuedHostedService: Error executing ValueTask: {item}.", workItem.Method.GetSignature());
            }
            finally
            {
                await ChangeValueTaskCounter(false);
            }
        }
    }

    /// <summary>
    /// Triggered when the application host is performing a graceful shutdown.
    /// </summary>
    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        if (_log)
            _logger.LogDebug("~~ QueuedHostedService: Stopping service...");

        await base.StopAsync(stoppingToken);
    }

    public async ValueTask<(int TaskLength, int ValueTaskLength)> GetCountOfProcessingTasks()
    {
        if (!_lockCounts)
            return (_taskProcessingCount, _valueTaskProcessingCount);

        using (await _asyncLock!.LockAsync())
        {
            return (_taskProcessingCount, _valueTaskProcessingCount);
        }
    }
}