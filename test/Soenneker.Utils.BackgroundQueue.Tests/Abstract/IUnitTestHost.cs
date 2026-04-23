using System;
using TUnit.Core.Interfaces;

namespace Soenneker.Utils.BackgroundQueue.Tests.Abstract;

/// <summary>
/// A minimal host for building and running tests.
/// </summary>
public interface IUnitTestHost : IAsyncInitializer, IAsyncDisposable
{
}