using Xunit;

namespace NServiceBus.Redis.Tests
{
    [CollectionDefinition("NServiceBusRedisFixture")]
    public class NServiceBusRedisFixtureCollection : ICollectionFixture<NServiceBusRedisFixture>
    {
    }
}
