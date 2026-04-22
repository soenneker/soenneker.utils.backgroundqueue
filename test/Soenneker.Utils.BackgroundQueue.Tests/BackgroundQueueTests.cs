using System.Threading.Tasks;
using Soenneker.Utils.BackgroundQueue.Abstract;
using Soenneker.Utils.Delay;

namespace Soenneker.Utils.BackgroundQueue.Tests;

[ClassDataSource<Host>(Shared = SharedType.PerTestSession)]
public class BackgroundQueueTests : HostedUnitTest
{
    private readonly IBackgroundQueue _util;

    public BackgroundQueueTests(Host host) : base(host)
    {
        _util = Resolve<IBackgroundQueue>();
    }

    private Task TestTask()
    {
        return Delay(1500, "test...");
    }

    private async ValueTask TestValueTask()
    {
        await Delay(1500, "test...");
    }

    [Test]
    public async Task WaitOnQueueToEmpty_should_complete_with_Task()
    {
        await _util.QueueTask(_ => TestTask(), CancellationToken);

        await WaitOnQueueToEmpty(CancellationToken);

        await DelayUtil.Delay(500, null, CancellationToken);
    }

    [Test]
    public async Task WaitOnQueueToEmpty_should_complete_with_ValueTask()
    {
        await _util.QueueValueTask(_ => TestValueTask(), CancellationToken);

        await WaitOnQueueToEmpty(CancellationToken);

        await DelayUtil.Delay(500, null, CancellationToken);
    }
}