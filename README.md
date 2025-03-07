[![](https://img.shields.io/nuget/v/Soenneker.Utils.BackgroundQueue.svg?style=for-the-badge)](https://www.nuget.org/packages/Soenneker.Utils.BackgroundQueue/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.utils.backgroundqueue/publish-package.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.utils.backgroundqueue/actions/workflows/publish-package.yml)
[![](https://img.shields.io/nuget/dt/Soenneker.Utils.BackgroundQueue.svg?style=for-the-badge)](https://www.nuget.org/packages/Soenneker.Utils.BackgroundQueue/)

# ![](https://user-images.githubusercontent.com/4441470/224455560-91ed3ee7-f510-4041-a8d2-3fc093025112.png) Soenneker.Utils.BackgroundQueue
### A high-performance background Task/ValueTask queue

## Overview
`BackgroundQueue` provides an efficient way to manage background task execution in .NET applications. It helps prevent application overload by processing tasks in a controlled, asynchronous manner.

## Features
- Supports both `ValueTask` and `Task` types.
- Configurable queue size to limit resource usage.
- Built-in tracking of running and pending tasks.
- Extension methods for easy setup and management.
- Includes a hosted service for automatic background processing.

## Installation

```sh
dotnet add package Soenneker.Utils.BackgroundQueue
```

Register the `BackgroundQueue`:
```csharp
void ConfigureServices(IServiceCollection services)
{
    services.AddBackgroundQueueAsSingleton();
}
```

### Starting
```csharp
await serviceProvider.WarmupAndStartBackgroundQueue(cancellationToken);
```

For synchronous start:
```csharp
serviceProvider.WarmupAndStartBackgroundQueueSync(cancellationToken);
```

### Stopping
To stop the service:
```csharp
await serviceProvider.StopBackgroundQueue(cancellationToken);
```

For synchronous stop:
```csharp
serviceProvider.StopBackgroundQueueSync(cancellationToken);
```

## Configuration
Configure the queue length and task tracking settings in your application:

```json
{
  "Background": {
    "QueueLength": 5000,
    "LockCounts": false,
    "Log": false
  }
}
```

- `QueueLength`: Defines the maximum number of tasks in the queue.
- `LockCounts`: Enables thread-safe counting of running tasks.
- `Log`: Outputs task tracking information to `ILogger`

## Initializing the Queue
To use `BackgroundQueue`, you probably want to inject it via your constructor:

```csharp
IBackgroundQueue _queue;

void MyClass(IBackgroundQueue queue)
{
    _queue = queue;
}
```

## Queueing Tasks

### Queuing a `ValueTask`
Rather than wrapping the task, you can elide it directly to avoid an extra state machine:
```csharp
await _queue.QueueValueTask(_ => someValueTask(), cancellationToken);
```

### Queuing a `Task`
Similarly, for `Task`:
```csharp
await _queue.QueueTask(_ => someTask(), cancellationToken);
```

## Waiting for Queue to Empty
To ensure all queued tasks finish before proceeding:
```csharp
await queue.WaitUntilEmpty(cancellationToken);
```

## Task Tracking
The queue tracks:
- The number of active `ValueTask` and `Task` instances.
- Whether any tasks are still processing.

To check if tasks are running:
```csharp
bool isProcessing = await queueInformationUtil.IsProcessing(cancellationToken);
```

To get current task counts:
```csharp
var (taskCount, valueTaskCount) = await queueInformationUtil.GetCountsOfProcessing(cancellationToken);
```