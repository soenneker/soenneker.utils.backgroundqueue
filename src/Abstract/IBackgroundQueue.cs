using System;
using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Utils.BackgroundQueue.Abstract;

/// <summary>
/// Adds Tasks and ValueTasks to the <see cref="QueuedHostedService"/>. <para/>
/// Must be Singleton IoC
/// </summary>
public interface IBackgroundQueue
{
    ValueTask QueueValueTask(Func<CancellationToken, ValueTask> workItem);

    ValueTask QueueTask(Func<CancellationToken, Task> workItem);

    ValueTask<Func<CancellationToken, ValueTask>> DequeueValueTask(CancellationToken cancellationToken);

    ValueTask<Func<CancellationToken, Task>> DequeueTask(CancellationToken cancellationToken);

    /// <summary>
    /// Returns the channel lengths via thread safe (and potentially locked) local variables
    /// </summary>
    ValueTask<(int TaskLength, int ValueTaskLength)> GetCountsOfChannels();
}