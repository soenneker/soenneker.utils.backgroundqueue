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
using Soenneker.Utils.BackgroundQueue.Dtos;

namespace Soenneker.Utils.BackgroundQueue;

/// <inheritdoc cref="IQueuedHostedService"/>
public sealed class QueuedHostedService : BackgroundService, IQueuedHostedService
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
    protected override Task ExecuteAsync(CancellationToken cancellationToken)
    {
        if (_log)
            _logger.LogDebug("~~ QueuedHostedService: Executing...");

        return ProcessQueue(cancellationToken);
    }

    private async Task ProcessQueue(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var dequeued = false;
            var isTask = false;
            string? workItemName = null;

            try
            {
                WorkItemEnvelope env = await _queue.Dequeue(cancellationToken).NoSync();
                dequeued = true;
                isTask = env.IsTask;

                if (_log)
                {
                    workItemName = env.Method?.GetSignature();
                    _logger.LogDebug("~~ QueuedHostedService: Starting {kind}: {item}", isTask ? "Task" : "ValueTask", workItemName);
                }

                await env.Invoke(cancellationToken).NoSync();

                if (_log)
                    _logger.LogDebug("~~ QueuedHostedService: Completed {kind}: {item}", isTask ? "Task" : "ValueTask", workItemName);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                if (dequeued)
                    _logger.LogError("~~ QueuedHostedService: Work item was cancelled while executing!: {item}", workItemName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "~~ QueuedHostedService: Error executing work item: {item}", workItemName);
            }
            finally
            {
                if (dequeued)
                {
                    if (isTask)
                        await _queueInformationUtil.DecrementTaskCounter(CancellationToken.None).NoSync();
                    else
                        await _queueInformationUtil.DecrementValueTaskCounter(CancellationToken.None).NoSync();
                }
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
