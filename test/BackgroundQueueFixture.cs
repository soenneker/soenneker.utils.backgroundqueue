using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Soenneker.Utils.BackgroundQueue.Extensions;
using Soenneker.Utils.BackgroundQueue.Registrars;
using Soenneker.Utils.Test;

namespace Soenneker.Utils.BackgroundQueue.Tests;

public class BackgroundQueueFixture : UnitFixture
{
    public override async Task InitializeAsync()
    {
        SetupIoC(Services);

        await base.InitializeAsync();

        ServiceProvider!.WarmupAndStartBackgroundQueue();
    }

    private static void SetupIoC(IServiceCollection services)
    {
        IConfiguration config = TestUtil.BuildConfig();

        services.TryAdd(ServiceDescriptor.Singleton<ILoggerFactory, LoggerFactory>());
        services.TryAdd(ServiceDescriptor.Singleton(typeof(ILogger<>), typeof(Logger<>)));

        services.AddSingleton(config);
        services.AddBackgroundQueue();
    }

    public override Task DisposeAsync()
    {
        ServiceProvider?.StopBackgroundQueue();

        return base.DisposeAsync();
    }
}