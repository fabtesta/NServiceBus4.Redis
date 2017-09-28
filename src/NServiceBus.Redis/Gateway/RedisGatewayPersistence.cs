using System;
using System.Collections.Generic;
using System.IO;
using NServiceBus.Gateway.Persistence;
using NServiceBus.Logging;
using NServiceBus.Redis.Extensions;
using ServiceStack.Redis;

namespace NServiceBus.Redis.Gateway
{
    public class RedisGatewayPersistence : IPersistMessages
    {
        static readonly ILog Logger = LogManager.GetLogger(typeof(RedisGatewayPersistence));

        public string EndpointName { get; set; }
        public int DefaultEntityTtl { get; set; }
        public IRedisClientsManager RedisClientsManager { get; set; }

        public RedisGatewayPersistence()
        {
            Logger.InfoFormat("RedisGatewayPersistence 2.x instance");
        }

        public RedisGatewayPersistence(IRedisClientsManager redisClientsManager)
        {
            RedisClientsManager = redisClientsManager;
            Logger.InfoFormat("RedisGatewayPersistence 2.x instance redisClientsManager {0}", redisClientsManager.GetType().FullName);
        }

        public RedisGatewayPersistence(string endpointName, IRedisClientsManager redisClientsManager, int defaultEntityTtl)
        {
            RedisClientsManager = redisClientsManager;
            DefaultEntityTtl = defaultEntityTtl;
            EndpointName = endpointName;

            Logger.InfoFormat("RedisGatewayPersistence 2.x instance endpointName {0} defaultEntityTtl {1} redisClientsManager {2}", endpointName, defaultEntityTtl, redisClientsManager.GetType().FullName);
        }

        public bool InsertMessage(string clientId, DateTime timeReceived, Stream messageStream, IDictionary<string, string> headers)
        {
            try
            {
                var gatewayMessage = new GatewayMessage
                {
                    Id = clientId.EscapeClientId(),
                    TimeReceived = timeReceived,
                    Headers = headers,
                    OriginalMessage = new byte[messageStream.Length],
                    Acknowledged = false
                };

                messageStream.Read(gatewayMessage.OriginalMessage, 0, (int)messageStream.Length);
                using (var redisClient = RedisClientsManager.GetClient())
                {
                    redisClient.As<GatewayMessage>().SetValue(EndpointName + gatewayMessage.Id, gatewayMessage, TimeSpan.FromMinutes(DefaultEntityTtl));
                    Logger.InfoFormat("Added gatewayMessage {0} with ttl {1}", gatewayMessage.Id, DefaultEntityTtl);
                }
                return true;
            }
            catch (Exception e)
            {
                Logger.Error(string.Format("ERROR InsertMessage {0}",clientId), e);
                throw;
            }
        }

        public bool AckMessage(string clientId, out byte[] message, out IDictionary<string, string> headers)
        {
            try
            {
                message = null;
                headers = null;

                using (var redisClient = RedisClientsManager.GetClient())
                {
                    var gatewayMessage = redisClient.As<GatewayMessage>().GetValue(EndpointName + clientId.EscapeClientId());

                    if (gatewayMessage == null)
                        throw new InvalidOperationException("No message with id: " + clientId + "found");
                    if (gatewayMessage.Acknowledged)
                        return false;

                    message = gatewayMessage.OriginalMessage;
                    headers = gatewayMessage.Headers;

                    gatewayMessage.Acknowledged = true;
                    redisClient.As<GatewayMessage>().SetValue(EndpointName + gatewayMessage.Id.EscapeClientId(), gatewayMessage, TimeSpan.FromMinutes(DefaultEntityTtl));

                    Logger.InfoFormat("Acked gatewayMessage {0}", gatewayMessage.Id);

                    return true;
                }
            }
            catch (Exception e)
            {
                Logger.Error(string.Format("ERROR AckMessage {0}", clientId), e);
                throw;
            }        
        }

        public void UpdateHeader(string clientId, string headerKey, string newValue)
        {
            try
            {
                using (var redisClient = RedisClientsManager.GetClient())
                {
                    var gatewayMessage = redisClient.As<GatewayMessage>().GetValue(EndpointName + clientId.EscapeClientId());

                    if (gatewayMessage == null)
                        throw new InvalidOperationException("No message with id: " + clientId + "found");
                    gatewayMessage.Headers[headerKey] = newValue;
                    redisClient.As<GatewayMessage>().SetValue(EndpointName + gatewayMessage.Id.EscapeClientId(), gatewayMessage, TimeSpan.FromMinutes(DefaultEntityTtl));

                    Logger.InfoFormat("UpdatedHeader gatewayMessage {0}", gatewayMessage.Id);                
                }
            }
            catch (Exception e)
            {
                Logger.Error(string.Format("ERROR UpdateHeader {0}", clientId), e);
                throw;
            }          
        }   
    }
}
