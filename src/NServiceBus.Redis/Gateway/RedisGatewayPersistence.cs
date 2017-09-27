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

        private readonly string _endpointName;
        private readonly int _defaultEntityTtl;
        private readonly IRedisClientsManager _redisClientsManager;

        public RedisGatewayPersistence(string endpointName, IRedisClientsManager redisClientsManager, int defaultEntityTtl)
        {
            _redisClientsManager = redisClientsManager;
            _defaultEntityTtl = defaultEntityTtl;
            _endpointName = endpointName;

            Logger.InfoFormat("RedisGatewayPersistence 2.x instance endpointName {0} defaultEntityTtl {1} redisClientsManager {2}", endpointName, defaultEntityTtl, redisClientsManager.GetType().FullName);
        }

        public bool InsertMessage(string clientId, DateTime timeReceived, Stream messageStream, IDictionary<string, string> headers)
        {
            try
            {
                var gatewayEntity = new GatewayEntity
                {
                    Id = clientId.EscapeClientId(),
                    TimeReceived = timeReceived,
                    Headers = headers,
                    OriginalMessage = new byte[messageStream.Length],
                    Acknowledged = false
                };

                messageStream.Read(gatewayEntity.OriginalMessage, 0, (int)messageStream.Length);
                using (var redisClient = _redisClientsManager.GetClient())
                {
                    redisClient.As<GatewayEntity>().SetValue(_endpointName + gatewayEntity.Id, gatewayEntity, TimeSpan.FromMinutes(_defaultEntityTtl));
                    Logger.DebugFormat("Added gatewayEntity {0} with ttl {1}", gatewayEntity.Id, _defaultEntityTtl);
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

                using (var redisClient = _redisClientsManager.GetClient())
                {
                    var gatewayEntity = redisClient.As<GatewayEntity>().GetValue(_endpointName + clientId.EscapeClientId());

                    if (gatewayEntity == null)
                        throw new InvalidOperationException("No message with id: " + clientId + "found");
                    if (gatewayEntity.Acknowledged)
                        return false;

                    message = gatewayEntity.OriginalMessage;
                    headers = gatewayEntity.Headers;

                    gatewayEntity.Acknowledged = true;
                    redisClient.As<GatewayEntity>().SetValue(_endpointName + gatewayEntity.Id.EscapeClientId(), gatewayEntity, TimeSpan.FromMinutes(_defaultEntityTtl));

                    Logger.DebugFormat("Acked gatewayEntity {0}", gatewayEntity.Id);

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
                using (var redisClient = _redisClientsManager.GetClient())
                {
                    var gatewayEntity = redisClient.As<GatewayEntity>().GetValue(_endpointName + clientId.EscapeClientId());

                    if (gatewayEntity == null)
                        throw new InvalidOperationException("No message with id: " + clientId + "found");
                    gatewayEntity.Headers[headerKey] = newValue;
                    redisClient.As<GatewayEntity>().SetValue(_endpointName + gatewayEntity.Id.EscapeClientId(), gatewayEntity, TimeSpan.FromMinutes(_defaultEntityTtl));

                    Logger.DebugFormat("UpdatedHeader gatewayEntity {0}", gatewayEntity.Id);                
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
