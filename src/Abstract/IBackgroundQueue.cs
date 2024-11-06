using System;
using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Utils.BackgroundQueue.Abstract;

/// <summary>
/// Interface for queuing tasks and value tasks to be processed by the <see cref="QueuedHostedService"/>.
/// </summary>
/// <remarks>
/// This service must be registered as a singleton in the IoC container.
/// </remarks>
public interface IBackgroundQueue
{
    /// <summary>
    /// Queues a <see cref="ValueTask"/> to be executed by the background service.
    /// </summary>
    /// <param name="workItem">A function representing the work item, which accepts a <see cref="CancellationToken"/> and returns a <see cref="ValueTask"/>.</param>
    /// <param name="cancellationToken">An optional token to cancel the operation.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
    ValueTask QueueValueTask(Func<CancellationToken, ValueTask> workItem, CancellationToken cancellationToken = default);

    /// <summary>
    /// Queues a <see cref="Task"/> to be executed by the background service.
    /// </summary>
    /// <param name="workItem">A function representing the work item, which accepts a <see cref="CancellationToken"/> and returns a <see cref="Task"/>.</param>
    /// <param name="cancellationToken">An optional token to cancel the operation.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
    ValueTask QueueTask(Func<CancellationToken, Task> workItem, CancellationToken cancellationToken = default);

    /// <summary>
    /// Dequeues a <see cref="ValueTask"/> from the queue for execution.
    /// </summary>
    /// <param name="cancellationToken">An optional token to cancel the operation.</param>
    /// <returns>A <see cref="ValueTask"/> containing a function that represents the dequeued work item, which accepts a <see cref="CancellationToken"/> and returns a <see cref="ValueTask"/>.</returns>
    ValueTask<Func<CancellationToken, ValueTask>> DequeueValueTask(CancellationToken cancellationToken = default);

    /// <summary>
    /// Dequeues a <see cref="Task"/> from the queue for execution.
    /// </summary>
    /// <param name="cancellationToken">An optional token to cancel the operation.</param>
    /// <returns>A <see cref="ValueTask"/> containing a function that represents the dequeued work item, which accepts a <see cref="CancellationToken"/> and returns a <see cref="Task"/>.</returns>
    ValueTask<Func<CancellationToken, Task>> DequeueTask(CancellationToken cancellationToken = default);
}