using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;

namespace Soenneker.Utils.BackgroundQueue.Abstract;

/// <summary>
/// Singleton IoC
/// </summary>
public interface IQueuedHostedService : IHostedService
{
    /// <summary>
    /// Returns the currently processing lengths via thread safe (and potentially locked) local variables
    /// </summary>
    ValueTask<(int TaskLength, int ValueTaskLength)> GetCountOfProcessingTasks();
}