using Xunit;

namespace Soenneker.Utils.BackgroundQueue.Tests;

/// <summary>
/// This class has no code, and is never created. Its purpose is simply
/// to be the place to apply [CollectionDefinition] and all the
/// <see cref="ICollectionFixture{TFixture}"/> interfaces.
/// </summary>
[CollectionDefinition("BackgroundQueueCollection")]
public class BackgroundQueueCollection : ICollectionFixture<BackgroundQueueFixture>
{
}