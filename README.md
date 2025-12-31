[![](https://img.shields.io/nuget/v/Soenneker.Utils.BackgroundQueue.svg?style=for-the-badge)](https://www.nuget.org/packages/Soenneker.Utils.BackgroundQueue/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.utils.backgroundqueue/publish-package.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.utils.backgroundqueue/actions/workflows/publish-package.yml)
[![](https://img.shields.io/nuget/dt/Soenneker.Utils.BackgroundQueue.svg?style=for-the-badge)](https://www.nuget.org/packages/Soenneker.Utils.BackgroundQueue/)

# ![](https://user-images.githubusercontent.com/4441470/224455560-91ed3ee7-f510-4041-a8d2-3fc093025112.png) Soenneker.Utils.BackgroundQueue

### A high-performance background Task / ValueTask queue

---

## Overview

`BackgroundQueue` provides a fast, controlled way to execute background work in .NET applications.
It prevents overload by queueing and processing work asynchronously with configurable limits and built-in tracking.

---

## Features

* Supports both `Task` and `ValueTask`
* Configurable queue size
* Tracks running and pending work
* Simple DI registration
* Hosted service for automatic background processing

---

## Installation

```sh
dotnet add package Soenneker.Utils.BackgroundQueue
```

Register the queue:

```csharp
void ConfigureServices(IServiceCollection services)
{
    services.AddBackgroundQueueAsSingleton();
}
```

---

## Starting & Stopping

### Start

```csharp
await serviceProvider.WarmupAndStartBackgroundQueue(cancellationToken);
```

Synchronous start:

```csharp
serviceProvider.WarmupAndStartBackgroundQueueSync(cancellationToken);
```

### Stop

```csharp
await serviceProvider.StopBackgroundQueue(cancellationToken);
```

Synchronous stop:

```csharp
serviceProvider.StopBackgroundQueueSync(cancellationToken);
```

---

## Configuration

```json
{
  "Background": {
    "QueueLength": 5000,
    "LockCounts": false,
    "Log": false
  }
}
```

* `QueueLength` – Maximum number of queued items
* `LockCounts` – Enables thread-safe tracking of running work
* `Log` – Enables debug logging

---

## Using the Queue

Inject `IBackgroundQueue`:

```csharp
IBackgroundQueue _queue;

void MyClass(IBackgroundQueue queue)
{
    _queue = queue;
}
```

### Queueing a `ValueTask`

```csharp
await _queue.QueueValueTask(_ => someValueTask(), cancellationToken);
```

### Queueing a `Task`

```csharp
await _queue.QueueTask(_ => someTask(), cancellationToken);
```

---

## ⚠️ Performance Tip: Prefer Stateful Queueing

Avoid capturing variables in lambdas when queueing work. Captured lambdas allocate and can impact performance under load.

### ❌ Avoid (captures state)

```csharp
await _queue.QueueTask(ct => DoWorkAsync(id, ct));
```

If `id` is a local variable, this creates a closure.

---

## ✅ Recommended: Pass State Explicitly

Use the stateful overloads with `static` lambdas.

### ValueTask

```csharp
await _queue.QueueValueTask(
    myService,
    static (svc, ct) => svc.ProcessAsync(ct),
    ct);
```

### Task

```csharp
await _queue.QueueTask(
    (logger, id),
    static (s, ct) => s.logger.RunAsync(s.id, ct),
    ct);
```

**Why this is better:**

* No closure allocations
* Lower GC pressure
* Best performance for high-throughput queues

The non-stateful overloads remain available for convenience, but **stateful queueing is recommended** for hot paths.

---

## Waiting for the Queue to Empty

```csharp
await queue.WaitUntilEmpty(cancellationToken);
```

---

## Task Tracking

Check if work is still processing:

```csharp
bool isProcessing = await queueInformationUtil.IsProcessing(cancellationToken);
```

Get current counts:

```csharp
var (taskCount, valueTaskCount) =
    await queueInformationUtil.GetCountsOfProcessing(cancellationToken);
```