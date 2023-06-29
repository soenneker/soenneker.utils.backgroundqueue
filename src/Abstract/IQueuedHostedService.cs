using Microsoft.Extensions.Hosting;

namespace Soenneker.Utils.BackgroundQueue.Abstract;

/// <summary>
/// Singleton IoC
/// </summary>
public interface IQueuedHostedService : IHostedService
{
}