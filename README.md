[![](https://img.shields.io/nuget/v/Soenneker.Utils.BackgroundQueue.svg?style=for-the-badge)](https://www.nuget.org/packages/Soenneker.Utils.BackgroundQueue/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.utils.backgroundqueue/publish-package.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.utils.backgroundqueue/actions/workflows/publish-package.yml)
[![](https://img.shields.io/nuget/dt/Soenneker.Utils.BackgroundQueue.svg?style=for-the-badge)](https://www.nuget.org/packages/Soenneker.Utils.BackgroundQueue/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.utils.backgroundqueue/codeql.yml?label=CodeQL&style=for-the-badge)](https://github.com/soenneker/soenneker.utils.backgroundqueue/actions/workflows/codeql.yml)

# Soenneker.Utils.BackgroundQueue

A bounded, single-consumer background queue for `Task` and `ValueTask` work items in hosted .NET applications.

## Installation

```bash
dotnet add package Soenneker.Utils.BackgroundQueue
```

## Registration

```csharp
builder.Services.AddBackgroundQueueAsSingleton();
```

Registration adds the queue, its counters, and its processor as singletons, and also registers the processor as a hosted service. A normal application host starts and stops it automatically; the manual start helpers are intended for test service providers that do not run hosted services.

Optional configuration:

```json
{
  "Background": {
    "QueueLength": 5000,
    "LockCounts": true,
    "Log": false
  }
}
```

- `QueueLength` is the bounded channel capacity. Values below `2` use the default of `5000`.
- `LockCounts` enables per-kind counts returned by `GetCountsOfProcessing()`. Overall processing state and `WaitUntilEmpty()` remain available either way.
- `Log` enables per-item debug logging.

## Queue work

```csharp
public sealed class ImportScheduler(IBackgroundQueue queue)
{
    public ValueTask QueueImport(string path, CancellationToken cancellationToken = default)
    {
        return queue.QueueValueTask(
            path,
            static (filePath, stoppingToken) => ImportAsync(filePath, stoppingToken),
            cancellationToken);
    }
}
```

The stateful overloads keep state separate from a static callback and avoid closure allocations. Non-stateful `QueueTask()` and `QueueValueTask()` overloads are also available.

Awaiting a queue call means the item was accepted by the bounded channel; it does not wait for that item to execute. When the channel is full, the call waits for capacity. The cancellation token passed to the queue call cancels that wait. During execution, callbacks receive the hosted service's stopping token.

Exceptions from a work item are logged and do not stop the processor. Work executes sequentially in enqueue order through the queue's single reader.

## Observe completion

```csharp
await queue.WaitUntilEmpty(cancellationToken);

bool busy = await queueInformation.IsProcessing(cancellationToken);
var (tasks, valueTasks) = await queueInformation.GetCountsOfProcessing(cancellationToken);
```

`WaitUntilEmpty()` includes both queued and currently executing work. Call it before host shutdown if queued work must complete: stopping the hosted service cancels the active callback and does not drain pending items.

Do not capture request-scoped services in work that may outlive the request scope. Queue stable state instead and create an appropriate service scope inside the callback when scoped dependencies are required.
