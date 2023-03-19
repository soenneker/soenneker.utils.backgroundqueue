using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soenneker.Utils.BackgroundQueue.Abstract;

namespace Soenneker.Utils.BackgroundQueue.Registrars;

public static class BackgroundQueueRegistrar
{
    /// <summary>
    /// Registers a high-performance background Task/ValueTask queue (Singleton)
    /// </summary>
    public static void AddBackgroundQueue(this IServiceCollection services)
    {
        services.TryAddSingleton<IQueuedHostedService, QueuedHostedService>();
        services.AddHostedService(svc => svc.GetService<IQueuedHostedService>()!); // TODO: TryAdd for HostedService
        services.TryAddSingleton<IBackgroundQueue, BackgroundQueue>();
    }
}