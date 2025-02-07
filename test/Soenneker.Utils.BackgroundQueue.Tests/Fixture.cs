using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Soenneker.Extensions.Task;
using Soenneker.Extensions.ValueTask;
using Soenneker.Utils.BackgroundQueue.Extensions;
using Soenneker.Utils.BackgroundQueue.Registrars;
using Soenneker.Utils.Test;

namespace Soenneker.Utils.BackgroundQueue.Tests;

public class Fixture : UnitFixture
{
    public override async ValueTask InitializeAsync()
    {
        SetupIoC(Services);

        await base.InitializeAsync().NoSync();

        await ServiceProvider!.WarmupAndStartBackgroundQueue().NoSync();
    }

    private static void SetupIoC(IServiceCollection services)
    {
        IConfiguration config = TestUtil.BuildConfig();

        services.TryAdd(ServiceDescriptor.Singleton<ILoggerFactory, LoggerFactory>());
        services.TryAdd(ServiceDescriptor.Singleton(typeof(ILogger<>), typeof(Logger<>)));

        services.AddSingleton(config);
        services.AddBackgroundQueueAsSingleton();
    }

    public override async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);

        if (ServiceProvider != null)
            await ServiceProvider.StopBackgroundQueue().NoSync();

        await base.DisposeAsync().NoSync();
    }
}