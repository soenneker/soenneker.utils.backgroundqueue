using System;
using Microsoft.Extensions.DependencyInjection;
using Soenneker.Utils.BackgroundQueue.Abstract;

namespace Soenneker.Utils.BackgroundQueue.Extensions;

public static class BackgroundQueueExtension
{
    /// <summary>
    /// Retrieves <see cref="IBackgroundQueue"/> from the serviceProvider, warming it up
    /// </summary>
    public static void WarmupBackgroundQueue(this IServiceProvider services)
    {
        services.GetService<IBackgroundQueue>();
    }
}