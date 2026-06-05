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
    public static Task WarmupAndStartBackgroundQueue(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        services.WarmupBackgroundQueue();
        return services.StartBackgroundQueue(cancellationToken);
    }

    /// <summary>
    /// Executes the warmup and start background queue sync operation.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public static void WarmupAndStartBackgroundQueueSync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        services.WarmupBackgroundQueue();
        services.StartBackgroundQueueSync(cancellationToken);
    }

    /// <summary>
    /// Executes the start background queue sync operation.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public static void StartBackgroundQueueSync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var queuedHostedService = services.GetService<IQueuedHostedService>();
        queuedHostedService!.StartAsync(cancellationToken);
    }

    /// <summary>
    /// Typically called in <code>Configure(IApplicationBuilder app)</code>
    /// </summary>
    /// <param name="services"></param>
    /// <param name="cancellationToken"></param>
    public static Task StartBackgroundQueue(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var queuedHostedService = services.GetService<IQueuedHostedService>();
        return queuedHostedService!.StartAsync(cancellationToken);
    }

    /// <summary>
    /// Executes the stop background queue sync operation.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public static void StopBackgroundQueueSync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var queuedHostedService = services.GetService<IQueuedHostedService>();

        if (queuedHostedService == null)
            return;

        queuedHostedService.StopAsync(cancellationToken).AwaitSync();
        queuedHostedService.Dispose();
    }

    /// <summary>
    /// Executes the stop background queue operation.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public static async ValueTask StopBackgroundQueue(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var queuedHostedService = services.GetService<IQueuedHostedService>();

        if (queuedHostedService == null)
            return;

        await queuedHostedService.StopAsync(cancellationToken).NoSync();
        queuedHostedService.Dispose();
    }
}