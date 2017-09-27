using Xunit;

namespace NServiceBus.Redis.AcceptanceTests
{
    [CollectionDefinition("NServiceBusAcceptanceTest")]
    public class NServiceBusAcceptanceTestCollection : ICollectionFixture<NServiceBusAcceptanceTest>
    {
    }
}
