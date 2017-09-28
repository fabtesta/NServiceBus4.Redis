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

        public string EndpointName { get; set; }
        public int DefaultEntityTtl { get; set; }
        public IRedisClientsManager RedisClientsManager { get; set; }

        public RedisDeduplication()
        {
            Logger.InfoFormat("RedisDeduplication 2.x instance");
        }

        public RedisDeduplication(IRedisClientsManager redisClientsManager)
        {
            RedisClientsManager = redisClientsManager;
            Logger.InfoFormat("RedisDeduplication 2.x instance redisClientsManager {0}", redisClientsManager.GetType().FullName);
        }

        public RedisDeduplication(string endpointName, IRedisClientsManager redisClientsManager, int defaultEntityTtl)
        {
            RedisClientsManager = redisClientsManager;
            DefaultEntityTtl = defaultEntityTtl;
            EndpointName = endpointName;

            Logger.InfoFormat("RedisDeduplication 2.x instance endpointName {0} defaultEntityTtl {1} redisClientsManager {2}", endpointName, defaultEntityTtl, redisClientsManager.GetType().FullName);
        }

        public bool DeduplicateMessage(string clientId, DateTime timeReceived)
        {
            try
            {
                using (var redisClient = RedisClientsManager.GetClient())
                {
                    var gatewayMessage = redisClient.As<GatewayMessage>().GetValue(EndpointName + clientId);

                    if (gatewayMessage != null)
                    {
                        Logger.InfoFormat("DeduplicatedMessage found gatewayMessage {0}", gatewayMessage.Id);
                        return false;
                    }

                    gatewayMessage = new GatewayMessage
                    {
                        Id = clientId.EscapeClientId(),
                        TimeReceived = timeReceived
                    };            
                
                    redisClient.As<GatewayMessage>().SetValue(EndpointName + gatewayMessage.Id, gatewayMessage, TimeSpan.FromMinutes(DefaultEntityTtl));
                    Logger.InfoFormat("DeduplicatedMessage added gatewayMessage {0} with ttl {1}", gatewayMessage.Id, DefaultEntityTtl);
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
