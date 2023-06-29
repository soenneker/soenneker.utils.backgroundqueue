using System.Threading.Tasks;
using Soenneker.Utils.BackgroundQueue.Abstract;
using Xunit;
using Xunit.Abstractions;

namespace Soenneker.Utils.BackgroundQueue.Tests;

[Collection("BackgroundQueueCollection")]
public class BackgroundQueueTests : FixturedUnitTest
{
    private readonly IBackgroundQueue _util;

    public BackgroundQueueTests(BackgroundQueueFixture fixture, ITestOutputHelper output) : base(fixture, output)
    {
        _util = Resolve<IBackgroundQueue>();
    }

    private async Task TestTask()
    {
        await Delay(1500, "test...");
    }

    private async ValueTask TestValueTask()
    {
        await Delay(1500, "test...");
    }

    [Fact]
    public async Task WaitOnQueueToEmpty_should_complete_with_Task()
    {
        await _util.QueueTask(_ => TestTask());

        await WaitOnQueueToEmpty();

        await Task.Delay(500);
    }

    [Fact]
    public async Task WaitOnQueueToEmpty_should_complete_with_ValueTask()
    {
        await _util.QueueValueTask(_ => TestValueTask());

        await WaitOnQueueToEmpty();

        await Task.Delay(500);
    }
}
