using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;

namespace Soenneker.Utils.BackgroundQueue.Abstract;

/// <summary>
/// Singleton IoC
/// </summary>
public interface IQueuedHostedService : IHostedService
{
    ValueTask<(int TaskLength, int ValueTaskLength)> GetCountOfProcessingTasks();
}