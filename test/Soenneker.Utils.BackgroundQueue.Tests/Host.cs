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

public class Host : UnitTestHost
{
    public override async Task InitializeAsync()
    {
        SetupIoC(Services);

        await base.InitializeAsync().NoSync();

        await ServicesProvider.WarmupAndStartBackgroundQueue().NoSync();
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
        await ServicesProvider.StopBackgroundQueue().NoSync();

        await base.DisposeAsync().NoSync();
    }
}
