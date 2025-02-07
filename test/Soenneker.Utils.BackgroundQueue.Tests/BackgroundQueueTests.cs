using System.Threading.Tasks;
using Soenneker.Utils.BackgroundQueue.Abstract;
using Xunit;


namespace Soenneker.Utils.BackgroundQueue.Tests;

[Collection("Collection")]
public class BackgroundQueueTests : FixturedUnitTest
{
    private readonly IBackgroundQueue _util;

    public BackgroundQueueTests(Fixture fixture, ITestOutputHelper output) : base(fixture, output)
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

    [Fact]
    public async Task WaitOnQueueToEmpty_should_complete_with_Task()
    {
        await _util.QueueTask(_ => TestTask(), CancellationToken);

        await WaitOnQueueToEmpty(CancellationToken);

        await Task.Delay(500, CancellationToken);
    }

    [Fact]
    public async Task WaitOnQueueToEmpty_should_complete_with_ValueTask()
    {
        await _util.QueueValueTask(_ => TestValueTask(), CancellationToken);

        await WaitOnQueueToEmpty(CancellationToken);

        await Task.Delay(500, CancellationToken);
    }
}
