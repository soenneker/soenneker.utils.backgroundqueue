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
    ValueTask<(int TaskLength, int ValueTaskLength)> GetCountsOfProcessing(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the currently processing lengths via thread safe (and potentially locked) local variables
    /// </summary>
    ValueTask<bool> IsProcessing(CancellationToken cancellationToken = default);

    /// <summary>
    /// Not to be called outside of <see cref="IBackgroundQueue"/> or <see cref="IQueuedHostedService"/>
    /// </summary>
    ValueTask<int> IncrementValueTaskCounter(CancellationToken cancellationToken = default);

    /// <summary>
    /// Not to be called outside of <see cref="IBackgroundQueue"/> or <see cref="IQueuedHostedService"/>
    /// </summary>
    ValueTask<int> DecrementValueTaskCounter(CancellationToken cancellationToken = default);

    /// <summary>
    /// Not to be called outside of <see cref="IBackgroundQueue"/> or <see cref="IQueuedHostedService"/>
    /// </summary>
    ValueTask<int> IncrementTaskCounter(CancellationToken cancellationToken = default);

    /// <summary>
    /// Not to be called outside of <see cref="IBackgroundQueue"/> or <see cref="IQueuedHostedService"/>
    /// </summary>
    ValueTask<int> DecrementTaskCounter(CancellationToken cancellationToken = default);
}