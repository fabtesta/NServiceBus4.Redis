using Xunit;

namespace NServiceBus.Redis.AcceptanceTests
{
    [CollectionDefinition("NServiceBusRedisAcceptanceTestFixture")]
    public class NServiceBusRedisAcceptanceTestCollection : ICollectionFixture<NServiceBusRedisAcceptanceTestFixture>
    {
    }
}
