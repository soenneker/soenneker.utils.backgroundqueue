using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Soenneker.Extensions.Task;
using Soenneker.Utils.BackgroundQueue.Abstract;

namespace Soenneker.Utils.BackgroundQueue.Extensions;

/// <summary>
/// Represents the background queue extension.
/// </summary>
public static class BackgroundQueueExtension
{
    /// <summary>
    /// Retrieves <see cref="IBackgroundQueue"/> from the <see cref="IServiceProvider"/>, warming it up
    /// </summary>
    public static void WarmupBackgroundQueue(this IServiceProvider services)
    {
        services.GetService<IBackgroundQueue>();
    }

    /// <summary>
    /// Retrieves <see cref="IBackgroundQueue"/> from the <see cref="IServiceProvider"/>, warming it up, and then starts it (typically in testing scenarios, this isn't necessary with WebApplicationFactory or regular apps)
    /// </summary>
    /// <returns>The <see cref="IBackgroundQueue"/> from the <see cref="IServiceProvider"/>, warming it up, and then starts it (typically in testing scenarios, this isn't necessary with WebApplicationFactory or regular apps).</returns>
    public static Task WarmupAndStartBackgroundQueue(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        services.WarmupBackgroundQueue();
        return services.StartBackgroundQueue(cancellationToken);
    }

    /// <summary>
    /// Resolves, warms, and starts the registered background queue synchronously.
    /// </summary>
    /// <param name="services">The service collection to resolve or update.</param>
    /// <param name="cancellationToken">Signals that the operation should stop.</param>
    public static void WarmupAndStartBackgroundQueueSync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        services.WarmupBackgroundQueue();
        services.StartBackgroundQueueSync(cancellationToken);
    }

    /// <summary>
    /// Starts the registered background queue synchronously.
    /// </summary>
    /// <param name="services">The service collection to resolve or update.</param>
    /// <param name="cancellationToken">Signals that the operation should stop.</param>
    public static void StartBackgroundQueueSync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var queuedHostedService = services.GetService<IQueuedHostedService>();
        queuedHostedService!.StartAsync(cancellationToken).AwaitSync();
    }

    /// <summary>
    /// Typically called in <code>Configure(IApplicationBuilder app)</code>
    /// </summary>
    /// <param name="services"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>Typically called in <code>Configure(IApplicationBuilder app)</code>.</returns>
    public static Task StartBackgroundQueue(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var queuedHostedService = services.GetService<IQueuedHostedService>();
        return queuedHostedService!.StartAsync(cancellationToken);
    }

    /// <summary>
    /// Stops the registered background queue synchronously and waits for queued shutdown work.
    /// </summary>
    /// <param name="services">The service collection to resolve or update.</param>
    /// <param name="cancellationToken">Signals that the operation should stop.</param>
    public static void StopBackgroundQueueSync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var queuedHostedService = services.GetService<IQueuedHostedService>();

        if (queuedHostedService == null)
            return;

        queuedHostedService.StopAsync(cancellationToken).AwaitSync();
        queuedHostedService.Dispose();
    }

    /// <summary>
    /// Stops the registered background queue asynchronously.
    /// </summary>
    /// <param name="services">The service collection to resolve or update.</param>
    /// <param name="cancellationToken">Signals that the operation should stop.</param>
    /// <returns>An awaitable that completes when the queue has stopped.</returns>
    public static async ValueTask StopBackgroundQueue(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var queuedHostedService = services.GetService<IQueuedHostedService>();

        if (queuedHostedService == null)
            return;

        await queuedHostedService.StopAsync(cancellationToken).NoSync();
        queuedHostedService.Dispose();
    }
}
