using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Soenneker.Utils.BackgroundQueue;
using Microsoft.Extensions.Configuration;

namespace Audit;

public class QueueSignalTests
{
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
    [Test]
    public async Task WaitersShareSignalAndCancellationDoesNotCancelOthers()
    {
        var info = new QueueInformationUtil(Fixture.Config());
        await info.IncrementTaskCounter();
        using var cts = new CancellationTokenSource();
        Task canceled = info.WaitUntilEmpty(cts.Token).AsTask();
        Task other = info.WaitUntilEmpty().AsTask();
        cts.Cancel();
        try { await canceled; throw new Exception("Cancellation was ignored"); }
        catch (OperationCanceledException) { }
        Check(!other.IsCompleted, "One waiter's cancellation affected another");
        await info.DecrementTaskCounter();
        await other.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task ConcurrentQueueWavesNeverLoseWakeups(bool trackCounts)
    {
        var info = new QueueInformationUtil(Fixture.Config(counts: trackCounts));
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var producers = Enumerable.Range(0, 4).Select(_ => Task.Run(async () =>
        {
            for (int i = 0; i < 4000; i++)
            {
                await info.IncrementTaskCounter();
                if ((i & 31) == 0) await Task.Yield();
                await info.DecrementTaskCounter();
            }
        })).ToArray();
        var waiters = Enumerable.Range(0, 4).Select(_ => Task.Run(async () =>
        {
            for (int i = 0; i < 1000; i++)
            {
                await info.WaitUntilEmpty(timeout.Token);
                await Task.Yield();
            }
        })).ToArray();
        await Task.WhenAll(producers.Concat(waiters)).WaitAsync(timeout.Token);
        Check(!await info.IsProcessing(), "Counters did not return to zero");
        Check(await info.GetCountsOfProcessing() == (0, 0), "Unbalanced counters");
    }
}

internal static class Fixture
{
    public static Microsoft.Extensions.Configuration.IConfiguration Config(bool logging = false, bool counts = true) =>
        new Microsoft.Extensions.Configuration.ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Azure:ServiceBus:Enable"] = "true", ["Azure:ServiceBus:TransmitterLogging"] = logging.ToString(),
            ["Background:QueueLength"] = "32", ["Background:LockCounts"] = counts.ToString()
        }).Build();
}
