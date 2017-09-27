using System;
using System.Collections.Generic;
using System.Linq;
using NServiceBus.Redis.Timeout;
using NServiceBus.Timeout.Core;
using ServiceStack.Redis;
using Xunit;

namespace NServiceBus.Redis.Test.Timeout
{
    [Trait("Category", "Integration")]
    public class RedisTimeoutPersistenceTests
    {
        private readonly IRedisClientsManager _redisClientsManager;
        private readonly RedisTimeoutPersistence _redisTimeoutPersistence;

        private readonly string endpointName = "nservicebus4-persistence:timeout:test";
        public RedisTimeoutPersistenceTests()
        {
            _redisClientsManager = ConfigureRedisPersistenceManager.GetRedisClientsManager();
            _redisTimeoutPersistence = new RedisTimeoutPersistence(endpointName, _redisClientsManager, 10);            
        }

        [Fact]
        public void Should_Insert_Item_To_SortedSet()
        {
            using (var redisClient = _redisClientsManager.GetClient())
            {
                redisClient.SortedSets[endpointName].Clear();
                _redisTimeoutPersistence.Add(new TimeoutData {Id = "Hello", Time = DateTime.UtcNow});
                Assert.Equal(1, redisClient.GetSortedSetCount(endpointName));
            }            
        }

        [Fact]
        public void Should_Return_Next_Query_DefaultTimeout()
        {
            using (var redisClient = _redisClientsManager.GetClient())
            {
                redisClient.SortedSets[endpointName].Clear();
                var time = DateTime.UtcNow;
                DateTime nextTimeToRun;
                var h = new Dictionary<string, string> {{"foo", "bar"}};
                _redisTimeoutPersistence.Add(new TimeoutData {Id = "Hello", Time = time, Headers = h});
                var chunk = _redisTimeoutPersistence.GetNextChunk(time.AddMinutes(-1), out nextTimeToRun);
                Assert.Equal(1, chunk.Count());
                Assert.InRange(chunk.First().Item2, time.AddSeconds(-1), time.AddSeconds(1));
                Assert.InRange(nextTimeToRun, time.AddMinutes(10), time.AddMinutes(10).AddSeconds(1));
            }
        }

        [Fact]
        public void Should_Return_Next_Query_Timeout()
        {
            using (var redisClient = _redisClientsManager.GetClient())
            {
                redisClient.SortedSets[endpointName].Clear();
                var time = DateTime.UtcNow;
                var nextTime = DateTime.UtcNow.AddDays(1);
                DateTime nextTimeToRun;
                var h = new Dictionary<string, string> {{"foo", "bar"}};
                _redisTimeoutPersistence.Add(new TimeoutData {Time = time, Headers = h});
                _redisTimeoutPersistence.Add(new TimeoutData {Time = nextTime, Headers = h});
                _redisTimeoutPersistence.GetNextChunk(DateTime.UtcNow.AddMinutes(-1), out nextTimeToRun);
                Assert.InRange(nextTimeToRun, nextTime.AddSeconds(-1), nextTime.AddSeconds(1));
            }
        }

        [Fact]
        public void Should_Remove_Item_From_SortedSet()
        {
            using (var redisClient = _redisClientsManager.GetClient())
            {
                redisClient.SortedSets[endpointName].Clear();
                var time = DateTime.UtcNow;
                DateTime nextTimeToRun;
                var h = new Dictionary<string, string> {{"foo", "bar"}};
                _redisTimeoutPersistence.Add(new TimeoutData {Time = time, Headers = h});
                var chunk = _redisTimeoutPersistence.GetNextChunk(DateTime.UtcNow.AddMinutes(-1), out nextTimeToRun);
                Assert.Equal(1, chunk.Count());
                TimeoutData timeoutData;
                var entry = _redisTimeoutPersistence.TryRemove(chunk.First().Item1, out timeoutData);
                Assert.True(entry);
                Assert.NotNull(timeoutData);
                Assert.Equal(h, timeoutData.Headers);
                var chunk2 = _redisTimeoutPersistence.GetNextChunk(DateTime.UtcNow.AddMinutes(-1), out nextTimeToRun);
                Assert.Equal(0, chunk2.Count());
            }
        }

        [Fact]
        public void Should_Remove_Item_From_SortedSet_By_Saga_Id()
        {
            using (var redisClient = _redisClientsManager.GetClient())
            {
                redisClient.SortedSets[endpointName].Clear();
                _redisTimeoutPersistence.Add(new TimeoutData { Time = DateTime.UtcNow, SagaId = Guid.Empty });
                Assert.Equal(1, redisClient.GetSortedSetCount(endpointName));
                _redisTimeoutPersistence.RemoveTimeoutBy(Guid.Empty);
                Assert.Equal(0, redisClient.GetSortedSetCount(endpointName));
            }           
        }
    }
}