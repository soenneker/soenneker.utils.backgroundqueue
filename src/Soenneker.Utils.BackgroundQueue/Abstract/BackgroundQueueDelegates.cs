using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Utils.BackgroundQueue.Abstract;

/// <summary>
/// Represents an asynchronous work item that operates on a provided state and supports cancellation.
/// </summary>
/// <remarks>Use this delegate to define asynchronous operations that require state and support
/// cancellation. The operation should observe the provided cancellation token and complete the returned ValueTask
/// when finished or canceled.</remarks>
/// <typeparam name="TState">The type of the state object to be passed to the work item.</typeparam>
/// <param name="state">The state object to be used by the work item. The type and meaning of this value are determined by the caller.</param>
/// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
/// <returns>A ValueTask that represents the asynchronous operation of the work item.</returns>
public delegate ValueTask ValueTaskWorkItem<in TState>(TState state, CancellationToken cancellationToken);

/// <summary>
/// Represents an asynchronous work item that executes with a specified state and supports cancellation.
/// </summary>
/// <remarks>The delegate should honor the cancellation request by observing the provided cancellation
/// token and responding appropriately if cancellation is requested.</remarks>
/// <typeparam name="TState">The type of the state object to be passed to the work item.</typeparam>
/// <param name="state">The state object to be used by the work item during execution.</param>
/// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
/// <returns>A task that represents the asynchronous operation.</returns>
public delegate Task TaskWorkItem<in TState>(TState state, CancellationToken cancellationToken);