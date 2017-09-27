using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Transactions;
using NServiceBus.Redis.Extensions;
using NServiceBus.Redis.Gateway;
using ServiceStack.Redis;
using Xunit;

namespace NServiceBus.Redis.Tests.Gateway
{
    [Trait("Category", "Integration")]
    public class RedisGatewayPersistenceTests
    {
        private readonly IRedisClientsManager _redisClientsManager;
        private readonly RedisGatewayPersistence _redisGatewayPersistence;

        private readonly string endpointName = "nservicebus4-persistence:gateway:test";
        public RedisGatewayPersistenceTests()
        {
            _redisClientsManager = ConfigureRedisPersistenceManager.GetRedisClientsManager();
            _redisGatewayPersistence = new RedisGatewayPersistence(endpointName, _redisClientsManager, 10);
        }

        [Fact]
        public void Should_Insert_Item_To_Redis()
        {
            using (var redisClient = _redisClientsManager.GetClient())
            {
                var entity = CreateTestGatewayEntity();
                using (var msgStream = new MemoryStream(entity.OriginalMessage))
                {
                    _redisGatewayPersistence.InsertMessage(entity.Id, entity.TimeReceived, msgStream, entity.Headers);
                    Assert.Equal(entity.Id.EscapeClientId(), redisClient.As<GatewayEntity>().GetValue(endpointName + entity.Id.EscapeClientId()).Id);
                }
            }
        }

        [Fact]
        public void Message_Should_Already_Been_Acked()
        {
            var entity = CreateTestGatewayEntity();
            using (var redisClient = _redisClientsManager.GetClient())
            {
               redisClient.As<GatewayEntity>().SetValue(endpointName + entity.Id.EscapeClientId(), entity, TimeSpan.FromMinutes(1));
            }
            
            byte[] msg;

            IDictionary<string, string> headers;
            _redisGatewayPersistence.AckMessage(entity.Id, out msg, out headers);

            Assert.False(_redisGatewayPersistence.AckMessage(entity.Id, out msg, out headers));
        }

        [Fact]
        public void Should_Return_Message_and_Headers()
        {
            var entity = CreateTestGatewayEntity();
            using (var redisClient = _redisClientsManager.GetClient())
            {
                redisClient.As<GatewayEntity>().SetValue(endpointName + entity.Id.EscapeClientId(), entity, TimeSpan.FromMinutes(1));
            }

            byte[] msg;

            IDictionary<string, string> headers;

            using (var tx = new TransactionScope())
            {
                _redisGatewayPersistence.AckMessage(entity.Id, out msg, out headers);
                tx.Complete();
            }

            Assert.Equal(entity.Headers, headers);
            Assert.Equal(entity.OriginalMessage, msg);
        }

        [Fact]
        public void Should_Ack_The_Message()
        {
            var entity = CreateTestGatewayEntity();
            using (var redisClient = _redisClientsManager.GetClient())
            {
                redisClient.As<GatewayEntity>().SetValue(endpointName + entity.Id.EscapeClientId(), entity, TimeSpan.FromMinutes(1));
            }

            byte[] msg;

            IDictionary<string, string> headers;

            _redisGatewayPersistence.AckMessage(entity.Id, out msg, out headers);

            using (var redisClient = _redisClientsManager.GetClient())
            {
                Assert.True(redisClient.As<GatewayEntity>().GetValue(endpointName + entity.Id.EscapeClientId()).Acknowledged);
            }            
        }

        [Fact]
        public void Should_Update_The_Header()
        {
            var entity = CreateTestGatewayEntity();
            using (var redisClient = _redisClientsManager.GetClient())
            {
                redisClient.As<GatewayEntity>().SetValue(endpointName + entity.Id.EscapeClientId(), entity, TimeSpan.FromMinutes(1));
            }

            var headerToUpdate = entity.Headers.First();

            _redisGatewayPersistence.UpdateHeader(entity.Id, headerToUpdate.Key, "Updated value");

            using (var redisClient = _redisClientsManager.GetClient())
            {
                Assert.Equal("Updated value", redisClient.As<GatewayEntity>().GetValue(endpointName + entity.Id.EscapeClientId()).Headers[headerToUpdate.Key]);
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
                Id = Guid.NewGuid() + "\\12345",
                TimeReceived = DateTime.UtcNow,
                Headers = headers,
                OriginalMessage = new byte[] { 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8 }
            };
        }
    }
}
