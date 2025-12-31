using Soenneker.Utils.BackgroundQueue.Dtos;
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
    /// Queues a work item represented by a ValueTask for execution, passing the specified state to the work item
    /// delegate.
    /// </summary>
    /// <typeparam name="TState">The type of the state object to pass to the work item.</typeparam>
    /// <param name="state">The state object to pass to the work item delegate when it is executed.</param>
    /// <param name="workItem">A delegate that represents the work to execute. The delegate receives the provided state object.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the queued work item before it starts executing. The default
    /// value is <see cref="CancellationToken.None"/>.</param>
    /// <returns>A ValueTask that represents the queued work item. The task completes when the work item has finished executing.</returns>
    ValueTask QueueValueTask<TState>(TState state, ValueTaskWorkItem<TState> workItem, CancellationToken cancellationToken = default);

    /// <summary>
    /// Queues a work item for execution, passing the specified state to the provided delegate.
    /// </summary>
    /// <typeparam name="TState">The type of the state object to pass to the work item.</typeparam>
    /// <param name="state">The state object to pass to the work item when it is executed.</param>
    /// <param name="workItem">A delegate that represents the work item to execute. The delegate receives the specified state object.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the queued work item before it starts executing. The default
    /// value is <see cref="CancellationToken.None"/>.</param>
    /// <returns>A <see cref="ValueTask"/> that represents the queued work item. The task completes when the work item has been
    /// executed or canceled.</returns>
    ValueTask QueueTask<TState>(TState state, TaskWorkItem<TState> workItem, CancellationToken cancellationToken = default);

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
    ValueTask<ValueTaskEnvelope> DequeueValueTask(CancellationToken cancellationToken = default);

    /// <summary>
    /// Dequeues a <see cref="Task"/> from the queue for execution.
    /// </summary>
    /// <param name="cancellationToken">An optional token to cancel the operation.</param>
    ValueTask<TaskEnvelope> DequeueTask(CancellationToken cancellationToken = default);

    /// <summary>
    /// This is really wait until both queues are empty and their work is done, not just are the queues empty.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    ValueTask WaitUntilEmpty(CancellationToken cancellationToken = default);
}