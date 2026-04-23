using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Soenneker.Extensions.ServiceProvider;
using Soenneker.Extensions.ValueTask;
using Soenneker.Tests.Logging;
using Soenneker.Tests.Unit;
using Soenneker.Utils.BackgroundQueue.Abstract;
using Soenneker.Utils.BackgroundQueue.Tests.Abstract;

namespace Soenneker.Utils.BackgroundQueue.Tests;

///<inheritdoc cref="IHostedUnitTest"/>
public abstract class HostedUnitTest : UnitTest, IHostedUnitTest
{
    public UnitTestHost Host { get; }

    public AsyncServiceScope? Scope { get; private set; }

    private readonly Lazy<IBackgroundQueue> _backgroundQueue;

    protected HostedUnitTest(UnitTestHost host) : base(host.AutoFaker, enableLogging: false)
    {
        Host = host ?? throw new ArgumentNullException(nameof(host));

        LazyLogger = new Lazy<ILogger<LoggingTest>>(() => Resolve<ILogger<HostedUnitTest>>(scoped: true), LazyThreadSafetyMode.ExecutionAndPublication);

        _backgroundQueue = new Lazy<IBackgroundQueue>(() => Resolve<IBackgroundQueue>(), LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public T Resolve<T>(bool scoped = false) where T : notnull
    {
        if (!scoped)
            return Host.ServicesProvider.Get<T>();

        Scope ??= Host.ServicesProvider.CreateAsyncScope();

        return Scope.Value.ServiceProvider.Get<T>();
    }

    public void CreateScope()
    {
        Scope ??= Host.ServicesProvider.CreateAsyncScope();
    }

    public ValueTask WaitOnQueueToEmpty(CancellationToken cancellationToken = default)
    {
        return _backgroundQueue.Value.WaitUntilEmpty(cancellationToken);
    }

    public override async ValueTask DisposeAsync()
    {
        if (Scope is not null)
        {
            await Scope.Value.DisposeAsync().NoSync();
            Scope = null;
        }

        await base.DisposeAsync().NoSync();
    }
}