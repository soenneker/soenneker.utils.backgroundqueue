using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soenneker.Utils.BackgroundQueue.Abstract;

namespace Soenneker.Utils.BackgroundQueue.Registrars;

/// <summary>
/// A high-performance background Task/ValueTask queue
/// </summary>
public static class BackgroundQueueRegistrar
{
    /// <summary>
    /// Tries to register a high-performance background Task/ValueTask queue (Singleton) as a HostedService
    /// </summary>
    public static void AddBackgroundQueue(this IServiceCollection services)
    {
        services.TryAddSingleton<IQueuedHostedService, QueuedHostedService>();
        services.AddHostedService(svc => svc.GetService<IQueuedHostedService>()!); // TODO: TryAdd for HostedService
        services.TryAddSingleton<IBackgroundQueue, BackgroundQueue>();
        services.TryAddSingleton<IQueueInformationUtil, QueueInformationUtil>();
    }
}