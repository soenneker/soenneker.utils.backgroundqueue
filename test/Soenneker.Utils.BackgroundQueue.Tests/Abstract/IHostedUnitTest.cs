using System;
using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Utils.BackgroundQueue.Tests.Abstract;

/// <summary>
/// A hosted test that provides synthetic inversion of control via <c>TestHost</c>. <para/>
/// Its most used function is <see cref="Resolve{T}"/>, which retrieves a service from the host service provider.
/// </summary>
public interface IHostedUnitTest : IAsyncDisposable
{
    /// <summary>
    /// Resolves a service from the host service provider.
    /// </summary>
    /// <remarks>
    /// Optionally creates a scope if needed, if one does not already exist.
    /// </remarks>
    T Resolve<T>(bool scoped = false) where T : notnull;

    /// <summary>
    /// Creates a scope for resolving scoped services.
    /// </summary>
    /// <remarks>
    /// Usually you will want to use <see cref="Resolve{T}"/> instead.
    /// </remarks>
    void CreateScope();

    /// <summary>
    /// Checks the background queue until it is empty.
    /// </summary>
    ValueTask WaitOnQueueToEmpty(CancellationToken cancellationToken = default);
}