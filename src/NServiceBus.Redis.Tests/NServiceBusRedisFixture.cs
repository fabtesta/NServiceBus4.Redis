using System;
using NServiceBus.Redis.Tests.Gateway;
using NServiceBus.Redis.Tests.Timeout;
using ServiceStack.Redis;

namespace NServiceBus.Redis.Tests
{
    public class NServiceBusRedisFixture : IDisposable
    {
        internal readonly IRedisClientsManager RedisClientsManager;

        public NServiceBusRedisFixture()
        {
            ////TEAR UP
            RedisClientsManager = ConfigureRedisPersistenceManager.GetRedisClientsManager();
        }

        public void Dispose()
        {
            ////TEAR DOWN
            using (var redisClient = RedisClientsManager.GetClient())
            {
                redisClient.SortedSets[RedisTimeoutPersistenceTests.EndpointName].Clear();
                var allKeys = redisClient.ScanAllKeys(RedisGatewayPersistenceTests.EndpointName+"*");
                redisClient.RemoveAll(allKeys); 
            }
            RedisClientsManager.Dispose();
        }
    }
}
