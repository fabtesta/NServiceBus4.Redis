using System;
using NServiceBus.Gateway.Deduplication;
using NServiceBus.Logging;
using NServiceBus.Redis.Extensions;
using ServiceStack.Redis;

namespace NServiceBus.Redis.Gateway
{
    public class RedisDeduplication : IDeduplicateMessages
    {
        static readonly ILog Logger = LogManager.GetLogger(typeof(RedisDeduplication));

        private readonly string _endpointName;
        private readonly int _defaultEntityTtl;
        private readonly IRedisClientsManager _redisClientsManager;

        public RedisDeduplication(string endpointName, IRedisClientsManager redisClientsManager, int defaultEntityTtl)
        {
            _redisClientsManager = redisClientsManager;
            _defaultEntityTtl = defaultEntityTtl;
            _endpointName = endpointName;

            Logger.InfoFormat("RedisDeduplication 2.x instance endpointName {0} defaultEntityTtl {1} redisClientsManager {2}", endpointName, defaultEntityTtl, redisClientsManager.GetType().FullName);
        }

        public bool DeduplicateMessage(string clientId, DateTime timeReceived)
        {
            try
            {
                using (var redisClient = _redisClientsManager.GetClient())
                {
                    var gatewayEntity = redisClient.As<GatewayEntity>().GetValue(_endpointName + clientId);

                    if (gatewayEntity != null)
                    {                    
                        return false;
                    }

                    gatewayEntity = new GatewayEntity
                    {
                        Id = clientId.EscapeClientId(),
                        TimeReceived = timeReceived
                    };            
                
                    redisClient.As<GatewayEntity>().SetValue(_endpointName + gatewayEntity.Id, gatewayEntity, TimeSpan.FromMinutes(_defaultEntityTtl));
                    Logger.DebugFormat("DeduplicatedMessage gatewayEntity {0} with ttl {1}", gatewayEntity.Id, _defaultEntityTtl);
                }
                return true;
            }
            catch (Exception e)
            {
                Logger.Error(string.Format("ERROR DeduplicateMessage {0}", clientId), e);
                throw;
            }     
        }        
    }
}
