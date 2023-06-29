using System.Threading.Tasks;
using Soenneker.Tests.Unit;
using Xunit;

namespace Soenneker.Utils.BackgroundQueue.Tests.Abstract;

/// <summary>
/// A fundamental xUnit test that stores UnitFixture and provides synthetic inversion of control. <para/>
/// It inherits from <see cref="UnitTest"/> and it's most used function is <see cref="Resolve{T}"/> which will reach out to the Fixture and retrieve a service from DI.
/// </summary>
public interface IFixturedUnitTest : IAsyncLifetime
{
    /// <summary>
    /// Syntactic sugar for Factory.Services.Get();
    /// </summary>
    /// <remarks>Optionally, creates a scope if needed (if one doesn't already exist)</remarks>
    T Resolve<T>(bool scoped = false);

    /// <summary>
    /// Needed for resolving scoped services. Don't need to worry about disposal, the end of the test handles that.
    /// </summary>
    /// <remarks>Usually you'll want to use <see cref="Resolve{T}"/></remarks>
    void CreateScope();

    /// <summary>
    /// Checks the background queue to see if it's empty, and loops until it is
    /// </summary>
    ValueTask WaitOnQueueToEmpty();
}