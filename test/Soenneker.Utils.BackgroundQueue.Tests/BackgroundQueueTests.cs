using System;
using System.Threading.Tasks;
using System.Threading;
using AwesomeAssertions;
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

    private Task TestTask(CancellationToken cancellationToken)
    {
        return Delay(1500, "test...", cancellationToken: cancellationToken);
    }

    private async ValueTask TestValueTask(CancellationToken cancellationToken)
    {
        await Delay(1500, "test...", cancellationToken: cancellationToken);
    }

    [Test]
    public async ValueTask WaitOnQueueToEmpty_should_complete_with_Task(CancellationToken cancellationToken)
    {
        await _util.QueueTask(_ => TestTask(cancellationToken), cancellationToken);

        await WaitOnQueueToEmpty(cancellationToken);

        await DelayUtil.Delay(500, null, cancellationToken);
    }

    [Test]
    public async ValueTask WaitOnQueueToEmpty_should_complete_with_ValueTask(CancellationToken cancellationToken)
    {
        await _util.QueueValueTask(_ => TestValueTask(cancellationToken), cancellationToken);

        await WaitOnQueueToEmpty(cancellationToken);

        await DelayUtil.Delay(500, null, cancellationToken);
    }

    [Test]
    public async ValueTask WaitUntilEmpty_should_wait_for_in_flight_generic_work_without_polling(CancellationToken cancellationToken)
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        await _util.QueueValueTask((started, release), static async (state, _) =>
        {
            state.started.SetResult();
            await state.release.Task;
        }, cancellationToken);

        try
        {
            // Other tests use this shared FIFO queue in parallel, so this item may legitimately wait behind their work.
            await started.Task.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken);

            ValueTask wait = _util.WaitUntilEmpty(cancellationToken);
            wait.IsCompleted.Should().BeFalse();

            release.TrySetResult();
            await wait.AsTask().WaitAsync(TimeSpan.FromSeconds(10), cancellationToken);
        }
        finally
        {
            // Never leave the hosted queue blocked if an assertion or timeout fails.
            release.TrySetResult();
        }
    }
}
