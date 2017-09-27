using System;
using System.Collections.Generic;
using System.Transactions;
using NServiceBus.Redis.Extensions;
using NServiceBus.Redis.Gateway;
using ServiceStack.Redis;
using Xunit;

namespace NServiceBus.Redis.Tests.Gateway
{
    [Trait("Category","Integration")]
    public class RedisDeduplicationTests
    {
        private readonly IRedisClientsManager _redisClientsManager;
        private readonly RedisDeduplication _redisDeduplication;

        private readonly string endpointName = "nservicebus4-persistence:gateway:test";
        public RedisDeduplicationTests()
        {
            _redisClientsManager = ConfigureRedisPersistenceManager.GetRedisClientsManager();
            _redisDeduplication = new RedisDeduplication(endpointName, _redisClientsManager, 10);
        }
        
        [Fact]
        public void Should_Deduplicate_The_Message()
        {
            var entity = CreateTestGatewayEntity();
            using (var redisClient = _redisClientsManager.GetClient())
            {
                redisClient.As<GatewayEntity>().SetValue(endpointName + entity.Id.EscapeClientId(), entity, TimeSpan.FromMinutes(1));
            }
            
            bool duplicated = false;
            using (var tx = new TransactionScope())
            {
                duplicated = _redisDeduplication.DeduplicateMessage(entity.Id.EscapeClientId(), DateTime.UtcNow);
                tx.Complete();
            }

            Assert.False(duplicated);
        }

        [Fact]
        public void Should_Add_Message_To_Avoid_Duplication()
        {
            var timeReceived = DateTime.UtcNow;
            timeReceived = timeReceived.AddMilliseconds(-timeReceived.Millisecond);
            timeReceived = timeReceived.AddTicks(-timeReceived.Ticks % TimeSpan.TicksPerSecond);

            var entity = CreateTestGatewayEntity();            
            bool duplicated = false;
            using (var tx = new TransactionScope())
            {
                duplicated = _redisDeduplication.DeduplicateMessage(entity.Id, timeReceived);
                tx.Complete();
            }

            Assert.True(duplicated);

            using (var redisClient = _redisClientsManager.GetClient())
            {
                var received = redisClient.As<GatewayEntity>().GetValue(endpointName + entity.Id.EscapeClientId()).TimeReceived;
                Assert.Equal(timeReceived, received);
            }
        }

        GatewayEntity CreateTestGatewayEntity()
        {
            var headers = new Dictionary<string, string>
            {
                {"Header1", "Value1"},
                {"Header2", "Value2"},
                {"Header3", "Value3"}
            };
            
            return new GatewayEntity
            {
                Id = Guid.NewGuid() + "\\67890",
                TimeReceived = DateTime.UtcNow,
                Headers = headers,
                OriginalMessage = new byte[] { 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8 }
            };
        }
    }
}
