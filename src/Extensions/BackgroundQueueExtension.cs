using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Soenneker.Extensions.Task;
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
    public static Task WarmupAndStartBackgroundQueue(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        services.WarmupBackgroundQueue();
        return services.StartBackgroundQueue(cancellationToken);
    }

    /// <inheritdoc cref="WarmupAndStartBackgroundQueue"/>
    public static void WarmupAndStartBackgroundQueueSync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        services.WarmupBackgroundQueue();
        services.StartBackgroundQueueSync(cancellationToken);
    }

    /// <inheritdoc cref="StartBackgroundQueue"/>
    /// <remarks>Hopefully one day this can be called async in main application lifetime flow.. https://github.com/dotnet/runtime/issues/65656</remarks>
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

    /// <inheritdoc cref="StopBackgroundQueue"/>
    public static void StopBackgroundQueueSync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var queuedHostedService = services.GetService<IQueuedHostedService>();

        if (queuedHostedService == null)
            return;

        queuedHostedService.StopAsync(cancellationToken).NoSync().GetAwaiter().GetResult();
        queuedHostedService.Dispose();
    }

    public static async ValueTask StopBackgroundQueue(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var queuedHostedService = services.GetService<IQueuedHostedService>();

        if (queuedHostedService == null)
            return;

        await queuedHostedService.StopAsync(cancellationToken).NoSync();
        queuedHostedService.Dispose();
    }
}