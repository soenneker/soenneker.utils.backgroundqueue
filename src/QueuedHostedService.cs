using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Soenneker.Extensions.MethodInfo;
using Soenneker.Extensions.Task;
using Soenneker.Extensions.ValueTask;
using Soenneker.Utils.BackgroundQueue.Abstract;

namespace Soenneker.Utils.BackgroundQueue;

/// <inheritdoc cref="IQueuedHostedService"/>
public class QueuedHostedService : BackgroundService, IQueuedHostedService
{
    private readonly IBackgroundQueue _queue;
    private readonly ILogger<QueuedHostedService> _logger;
    private readonly IQueueInformationUtil _queueInformationUtil;

    private readonly bool _log;

    public QueuedHostedService(IConfiguration config, IBackgroundQueue queue, ILogger<QueuedHostedService> logger, IQueueInformationUtil queueInformationUtil)
    {
        _log = config.GetValue<bool>("Background:Log");

        _queue = queue;
        _logger = logger;
        _queueInformationUtil = queueInformationUtil;
    }

    /// <summary>
    /// Needs calling manually from unit test fixtures to start it
    /// </summary>
    public override Task StartAsync(CancellationToken cancellationToken)
    {
        if (_log)
            _logger.LogDebug("~~ QueuedHostedService: Starting...");

        return base.StartAsync(cancellationToken);
    }

    /// <summary>
    /// Triggered when the application host is ready to start the service.
    /// </summary>
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_log)
            _logger.LogDebug("~~ QueuedHostedService: Executing...");

        Task valueTaskProcessing = ValueTaskProcessing(stoppingToken);
        Task taskProcessing = TaskProcessing(stoppingToken);

        return Task.WhenAll(valueTaskProcessing, taskProcessing);
    }

    private async Task TaskProcessing(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            Func<CancellationToken, Task>? workItem = null;

            try
            {
                string? workItemName = null;

                workItem = await _queue.DequeueTask(stoppingToken).NoSync();

                if (_log)
                {
                    workItemName = workItem.Method.GetSignature();
                    _logger.LogDebug("~~ QueuedHostedService: Starting Task: {item}", workItemName);
                }

                await workItem(stoppingToken).NoSync();

                if (_log)
                    _logger.LogDebug("~~ QueuedHostedService: Completed Task: {item}", workItemName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "~~ QueuedHostedService:: Error executing Task: {item}", workItem?.Method.GetSignature());
            }
            finally
            {
                await _queueInformationUtil.DecrementTaskCounter(stoppingToken).NoSync();
            }
        }
    }

    private async Task ValueTaskProcessing(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            Func<CancellationToken, ValueTask>? workItem = null;

            try
            {
                string? workItemName = null;

                workItem = await _queue.DequeueValueTask(stoppingToken).NoSync();

                if (_log)
                {
                    workItemName = workItem.Method.GetSignature();
                    _logger.LogDebug("~~ QueuedHostedService: Starting ValueTask: {item}", workItemName);
                }

                await workItem(stoppingToken).NoSync();

                if (_log)
                    _logger.LogDebug("~~ QueuedHostedService: Completed ValueTask: {item}", workItemName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "~~ QueuedHostedService: Error executing ValueTask: {item}", workItem?.Method.GetSignature());
            }
            finally
            {
                await _queueInformationUtil.DecrementValueTaskCounter(stoppingToken).NoSync();
            }
        }
    }

    /// <summary>
    /// Triggered when the application host is performing a graceful shutdown.
    /// </summary>
    public override Task StopAsync(CancellationToken stoppingToken)
    {
        if (_log)
            _logger.LogDebug("~~ QueuedHostedService: Stopping service...");

        return base.StopAsync(stoppingToken);
    }
}