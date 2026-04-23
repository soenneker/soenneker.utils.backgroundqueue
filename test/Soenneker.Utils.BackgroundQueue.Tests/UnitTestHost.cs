using System;
using System.Linq;
using System.Threading.Tasks;
using Bogus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Extensions.Logging;
using Soenneker.Asyncs.Initializers;
using Soenneker.Atomics.ValueBools;
using Soenneker.Extensions.ValueTask;
using Soenneker.Serilog.Sinks.TUnit;
using Soenneker.Utils.AutoBogus;
using Soenneker.Utils.AutoBogus.Config;
using Soenneker.Utils.BackgroundQueue.Tests.Abstract;

namespace Soenneker.Utils.BackgroundQueue.Tests;

///<inheritdoc cref="IUnitTestHost"/>
public class UnitTestHost : IUnitTestHost
{
    private ServiceProvider? _serviceProvider;
    private ILoggerFactory? _loggerFactory;
    private SerilogLoggerProvider? _serilogProvider;
    private global::Serilog.Core.Logger? _serilogLogger;
    private TUnitTestContextSink? _tUnitSink;

    private ValueAtomicBool _disposed;
    private readonly AsyncInitializer _initializer;

    private readonly Lazy<AutoFaker> _autoFaker;
    private readonly Lazy<Faker> _faker;

    public IServiceCollection Services { get; } = new ServiceCollection();

    public IServiceProvider ServicesProvider =>
        _serviceProvider ?? throw new InvalidOperationException("Host has not been initialized. Call Initialize() first.");

    public Faker Faker => _faker.Value;

    public AutoFaker AutoFaker => _autoFaker.Value;

    public UnitTestHost()
    {
        _faker = new Lazy<Faker>(() => new Faker(), true);
        _autoFaker = new Lazy<AutoFaker>(() =>
        {
            var config = new AutoFakerConfig();
            return new AutoFaker(config);
        }, true);

        _initializer = new AsyncInitializer(BuildServices);
    }

    public virtual async Task InitializeAsync()
    {
        await _initializer.Init().NoSync();
    }

    private ValueTask BuildServices()
    {
        EnsureLoggingConfigured();
        _serviceProvider = Services.BuildServiceProvider(validateScopes: true);
        return ValueTask.CompletedTask;
    }

    private void EnsureLoggingConfigured()
    {
        if (_serilogLogger is null)
        {
            _tUnitSink = new TUnitTestContextSink();

            _serilogLogger = new LoggerConfiguration().MinimumLevel.Verbose().Enrich.FromLogContext().WriteTo.Sink(_tUnitSink).CreateLogger();

            _serilogProvider = new SerilogLoggerProvider(_serilogLogger, dispose: false);
            _loggerFactory = LoggerFactory.Create(builder => builder.AddProvider(_serilogProvider));
        }

        Log.Logger = _serilogLogger;

        if (Services.All(descriptor => descriptor.ServiceType != typeof(ILoggerFactory)))
            Services.AddSingleton(_loggerFactory!);

        if (Services.All(descriptor => descriptor.ServiceType != typeof(ILogger<>)))
            Services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));
    }

    public virtual async ValueTask DisposeAsync()
    {
        if (!_disposed.TrySetTrue())
            return;

        if (_serviceProvider is IAsyncDisposable asyncDisposable)
            await asyncDisposable.DisposeAsync().NoSync();
        else
        {
            if (_serviceProvider != null)
                await _serviceProvider.DisposeAsync().NoSync();
        }

        if (_serilogProvider is not null)
            await _serilogProvider.DisposeAsync().NoSync();

        _loggerFactory?.Dispose();

        if (_serilogLogger is not null)
            await _serilogLogger.DisposeAsync().NoSync();

        if (_tUnitSink is not null)
            await _tUnitSink.DisposeAsync().NoSync();

        await _initializer.DisposeAsync().NoSync();

        Log.Logger = global::Serilog.Core.Logger.None;
    }
}
