using Microsoft.Extensions.Hosting;
using System;

namespace Soenneker.Utils.BackgroundQueue.Abstract;

/// <summary>
/// Singleton IoC
/// </summary>
public interface IQueuedHostedService : IHostedService, IDisposable
{
}