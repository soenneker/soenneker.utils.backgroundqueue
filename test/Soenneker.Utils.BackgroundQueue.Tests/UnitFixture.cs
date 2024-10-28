using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Sinks.XUnit.Injectable;
using Serilog.Sinks.XUnit.Injectable.Abstract;
using Serilog.Sinks.XUnit.Injectable.Extensions;
using Soenneker.Extensions.ValueTask;
using Xunit;

namespace Soenneker.Utils.BackgroundQueue.Tests;

/// <summary>
/// A base xUnit fixture providing injectable log output and DI mechanisms like IServiceCollection and ServiceProvider
/// </summary>
/// <remarks>Does not use Soenneker.Fixtures.Unit because it'll result in circular dependency</remarks>
public abstract class UnitFixture : IAsyncLifetime
{
    public ServiceProvider? ServiceProvider { get; set; }

    protected IServiceCollection Services { get; set; }

    public UnitFixture()
    {
        // this needs to remain in constructor because of derivations
        Services = new ServiceCollection();

        var injectableTestOutputSink = new InjectableTestOutputSink();

        Services.AddSingleton<IInjectableTestOutputSink>(injectableTestOutputSink);

        ILogger serilogLogger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.InjectableTestOutput(injectableTestOutputSink)
            .Enrich.FromLogContext()
            .CreateLogger();

        Log.Logger = serilogLogger;
    }

    public virtual Task InitializeAsync()
    {
        ServiceProvider = Services.BuildServiceProvider();

        return Task.CompletedTask;
    }
    
    public virtual async Task DisposeAsync()
    {
        GC.SuppressFinalize(this);

        if (ServiceProvider != null)
            await ServiceProvider.DisposeAsync().NoSync();
    }
}