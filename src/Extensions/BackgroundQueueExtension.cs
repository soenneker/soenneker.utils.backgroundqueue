using System;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Soenneker.Utils.BackgroundQueue.Abstract;

namespace Soenneker.Utils.BackgroundQueue.Extensions;

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
    public static void WarmupAndStartBackgroundQueue(this IServiceProvider services)
    {
        services.GetService<IBackgroundQueue>();

        var queuedHostedService = services.GetService<IQueuedHostedService>();
        queuedHostedService!.StartAsync(CancellationToken.None);
    }

    public static void StopBackgroundQueue(this IServiceProvider services)
    {
        var queuedHostedService = services.GetService<IQueuedHostedService>();

        queuedHostedService?.StopAsync(CancellationToken.None);
    }
}