using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Utils.BackgroundQueue.Abstract;

/// <summary>
/// Allows for retrieval of information about <see cref="IBackgroundQueue"/> and <see cref="IQueuedHostedService"/> (such as are there currently Tasks/ValueTasks being processed)
/// </summary>
public interface IQueueInformationUtil
{
    /// <summary>
    /// Returns the currently processing lengths via thread safe (and potentially locked) local variables
    /// </summary>
    /// <returns>The currently processing lengths via thread safe (and potentially locked) local variables.</returns>
    ValueTask<(int TaskLength, int ValueTaskLength)> GetCountsOfProcessing(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the currently processing lengths via thread safe (and potentially locked) local variables
    /// </summary>
    /// <returns>The currently processing lengths via thread safe (and potentially locked) local variables.</returns>
    ValueTask<bool> IsProcessing(CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously waits for all queued and currently executing work to complete.
    /// </summary>
    /// <returns>Asynchronously waits for all queued and currently executing work to complete.</returns>
    ValueTask WaitUntilEmpty(CancellationToken cancellationToken = default);

    /// <summary>
    /// Not to be called outside of <see cref="IBackgroundQueue"/> or <see cref="IQueuedHostedService"/>
    /// </summary>
    /// <returns>Not to be called outside of <see cref="IBackgroundQueue"/> or <see cref="IQueuedHostedService"/>.</returns>
    ValueTask<int> IncrementValueTaskCounter(CancellationToken cancellationToken = default);

    /// <summary>
    /// Not to be called outside of <see cref="IBackgroundQueue"/> or <see cref="IQueuedHostedService"/>
    /// </summary>
    /// <returns>Not to be called outside of <see cref="IBackgroundQueue"/> or <see cref="IQueuedHostedService"/>.</returns>
    ValueTask<int> DecrementValueTaskCounter(CancellationToken cancellationToken = default);

    /// <summary>
    /// Not to be called outside of <see cref="IBackgroundQueue"/> or <see cref="IQueuedHostedService"/>
    /// </summary>
    /// <returns>Not to be called outside of <see cref="IBackgroundQueue"/> or <see cref="IQueuedHostedService"/>.</returns>
    ValueTask<int> IncrementTaskCounter(CancellationToken cancellationToken = default);

    /// <summary>
    /// Not to be called outside of <see cref="IBackgroundQueue"/> or <see cref="IQueuedHostedService"/>
    /// </summary>
    /// <returns>Not to be called outside of <see cref="IBackgroundQueue"/> or <see cref="IQueuedHostedService"/>.</returns>
    ValueTask<int> DecrementTaskCounter(CancellationToken cancellationToken = default);
}
