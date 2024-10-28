using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Extensions.Logging;
using Serilog.Sinks.XUnit.Injectable.Abstract;
using Soenneker.Extensions.ServiceProvider;
using Soenneker.Extensions.ValueTask;
using Soenneker.Tests.Logging;
using Soenneker.Tests.Unit;
using Soenneker.Utils.BackgroundQueue.Abstract;
using Soenneker.Utils.BackgroundQueue.Tests.Abstract;
using Xunit.Abstractions;

namespace Soenneker.Utils.BackgroundQueue.Tests;

///<inheritdoc cref="IFixturedUnitTest"/>
public class FixturedUnitTest : UnitTest, IFixturedUnitTest
{
    public UnitFixture Fixture { get; }

    public AsyncServiceScope? Scope { get; private set; }

    private readonly Lazy<IQueueInformationUtil> _queueInformationUtil;

    public FixturedUnitTest(UnitFixture fixture, ITestOutputHelper testOutputHelper)
    {
        Fixture = fixture;

        var outputSink = Resolve<IInjectableTestOutputSink>();
        outputSink.Inject(testOutputHelper);

        _queueInformationUtil = new Lazy<IQueueInformationUtil>(() => Resolve<IQueueInformationUtil>(), LazyThreadSafetyMode.ExecutionAndPublication);

        LazyLogger = new Lazy<ILogger<LoggingTest>>(BuildLogger<FixturedUnitTest>, LazyThreadSafetyMode.ExecutionAndPublication);
    }

    /// <summary>
    /// Uses the static Serilog Log.Logger, and returns Microsoft ILogger after building a new one. Avoid if you can, utilize DI!
    /// Serilog should be configured with applicable sinks before calling this
    /// </summary>
    public static ILogger<T> BuildLogger<T>()
    {
        ILogger<T> logger = new SerilogLoggerFactory(Log.Logger).CreateLogger<T>();

        return logger;
    }

    public T Resolve<T>(bool scoped = false)
    {
        if (Fixture.ServiceProvider == null)
            throw new Exception($"ServiceProvider was null trying to resolve service {typeof(T).Name}! Not able to resolve service");

        if (!scoped)
            return Fixture.ServiceProvider.Get<T>();

        if (Scope == null)
            CreateScope();

        return Scope!.Value.ServiceProvider.Get<T>();
    }

    public void CreateScope()
    {
        if (Fixture.ServiceProvider == null)
            throw new Exception("ServiceProvider was null trying create a scope!");

        Scope = Fixture.ServiceProvider.CreateAsyncScope();
    }
    
    public async ValueTask WaitOnQueueToEmpty()
    {
        const int delayMs = 500;

        bool isProcessing;

        do
        {
            isProcessing = await _queueInformationUtil.Value.IsProcessing().NoSync();

            if (isProcessing)
            {
                await Delay(delayMs, "Background queue emptying...", false);
            }
            else
            {
                Logger.LogDebug("Background queue is empty; continuing");
            }
        } while (isProcessing);
    }

    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        GC.SuppressFinalize(this);

        if (Scope != null)
            await Scope.Value.DisposeAsync().NoSync();
    }
}