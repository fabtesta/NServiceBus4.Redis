using System;
using System.Collections.Generic;
using System.Linq;
using NServiceBus.Logging;
using NServiceBus.Redis.Extensions;
using NServiceBus.Timeout.Core;
using ServiceStack;
using ServiceStack.Redis;

namespace NServiceBus.Redis.Timeout
{
    public class RedisTimeoutPersistence : IPersistTimeouts
    {
        static readonly ILog Logger = LogManager.GetLogger(typeof(RedisTimeoutPersistence));                

        internal static readonly string TimeoutEntityName = "TimeoutData";
        private readonly string _endpointName;
        private readonly int _defaultPollingTimeout;
        private readonly IRedisClientsManager _redisClientsManager;
        
        public RedisTimeoutPersistence(string endpointName, IRedisClientsManager redisClientsManager, int defaultPollingTimeout)
        {
            _endpointName = endpointName;
            _redisClientsManager = redisClientsManager;
            _defaultPollingTimeout = defaultPollingTimeout;
            
            Logger.InfoFormat("RedisTimeoutPersistence instance endpointName {0} defaultPollingTimeout {1} redisClientsManager {2}", endpointName,  defaultPollingTimeout, redisClientsManager.GetType().FullName);
            
        }

        public List<Tuple<string, DateTime>> GetNextChunk(DateTime startSlice, out DateTime nextTimeToRunQuery)
        {
            var endSlice = DateTime.UtcNow;
            Logger.DebugFormat("GetNextChunk from {0} to {1} with default polling timeout {2}", startSlice.ToUnixTimeSeconds(),  endSlice.ToUnixTimeSeconds(), TimeSpan.FromMinutes(_defaultPollingTimeout));
            using (var redisClient = _redisClientsManager.GetClient())
            {
                var timeoutRange = redisClient.As<TimeoutEntity>().SortedSets[_endpointName].GetRangeByLowestScore(startSlice.ToUnixTimeSeconds(),  endSlice.ToUnixTimeSeconds()).Select(r => Tuple.Create(r.Id, r.Time)).ToList();
                var nextTimeoutRange = redisClient.As<TimeoutEntity>().SortedSets[_endpointName].GetRangeByLowestScore(endSlice.ToUnixTimeSeconds() + 1, DateTime.MaxValue.ToUnixTimeSeconds()).Select(r => r.Time).ToList();                
                if (nextTimeoutRange.Any())
                {
                    nextTimeToRunQuery = nextTimeoutRange.First();
                }
                else
                {
                    nextTimeToRunQuery = endSlice.AddMinutes(_defaultPollingTimeout);
                }
                Logger.DebugFormat("GetNextChunk timeoutRange {0} nextTimeoutRange {1} nextTimeToRunQuery {2}", timeoutRange.Count,  nextTimeoutRange.Count, nextTimeToRunQuery);
                return timeoutRange;
            }            
        }

        public void Add(TimeoutData timeoutData)
        {
            timeoutData.Id = Guid.NewGuid().ToString();
            Logger.DebugFormat("Add timeoutData {0}", timeoutData.Id);
            var timeoutEntity = new TimeoutEntity
            {
                Id = timeoutData.Id,
                Destination = timeoutData.Destination != null ? timeoutData.Destination.ToString() : null,
                SagaId = timeoutData.SagaId,
                State = timeoutData.State,
                Time = timeoutData.Time,
                TimeString = timeoutData.Time.ToString("O"),
                Headers = timeoutData.Headers,
                CorrelationId = timeoutData.CorrelationId,
                OwningTimeoutManager = timeoutData.OwningTimeoutManager                
            };
            
            using (var redisClient = _redisClientsManager.GetClient())
            {
                redisClient.As<TimeoutEntity>().AddItemToSortedSet(redisClient.As<TimeoutEntity>().SortedSets[_endpointName], timeoutEntity, timeoutEntity.Time.ToUnixTimeSeconds());              
                Logger.DebugFormat("Added timeoutEntity {0} with score {1} to sorted set {2}", timeoutEntity.Id, timeoutEntity.Time.ToUnixTimeSeconds(), redisClient.As<TimeoutEntity>().SortedSets[_endpointName].ToUrn());            
            }
        }

        public bool TryRemove(string timeoutId, out TimeoutData timeoutData)
        {
            using (var redisClient = _redisClientsManager.GetClient())
            {
                var timeoutEntity = redisClient.As<TimeoutEntity>().SortedSets[_endpointName].GetAll().FirstOrDefault(x => x.Id == timeoutId);
                if (timeoutEntity == null)
                    timeoutData = default(TimeoutData);
                else
                {
                    redisClient.As<TimeoutEntity>().SortedSets[_endpointName].Remove(timeoutEntity);
                    timeoutData = new TimeoutData
                    {
                        Id = timeoutEntity.Id,
                        Destination = timeoutEntity.Destination != null ? Address.Parse(timeoutEntity.Destination) : null,
                        SagaId = timeoutEntity.SagaId,
                        State = timeoutEntity.State,
                        Time = timeoutEntity.Time,
                        Headers = timeoutEntity.Headers,
                        CorrelationId = timeoutEntity.CorrelationId,
                        OwningTimeoutManager = timeoutEntity.OwningTimeoutManager
                    };
                }
                
                Logger.DebugFormat("TryRemove timeoutData {0} from sorted set {1}. Found? {2}", timeoutId, redisClient.As<TimeoutEntity>().SortedSets[_endpointName].ToUrn(), timeoutData != default(TimeoutData));
            }
            
            return timeoutData != default(TimeoutData);            
        }

        public void RemoveTimeoutBy(Guid sagaId)
        {
            using (var redisClient = _redisClientsManager.GetClient())
            {
                var timeoutEntity = redisClient.As<TimeoutEntity>().SortedSets[_endpointName].GetAll().FirstOrDefault(x => x.SagaId == sagaId);                                
                redisClient.As<TimeoutEntity>().SortedSets[_endpointName].Remove(timeoutEntity);
                Logger.DebugFormat("RemoveTimeoutBy sagaId {0} from sorted set {1}. Found? {2}", sagaId, redisClient.As<TimeoutEntity>().SortedSets[_endpointName].ToUrn(), timeoutEntity != null);
            }
        }
    }
}