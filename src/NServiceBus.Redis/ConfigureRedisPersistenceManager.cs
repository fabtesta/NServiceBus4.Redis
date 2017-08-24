using System.Configuration;
using NServiceBus.Redis.Timeout;
using NServiceBus.Timeout.Core;
using ServiceStack;
using ServiceStack.Redis;

namespace NServiceBus.Redis
{
    public static class ConfigureRedisPersistenceManager
    {
        private static readonly object SyncObj = new object();
        private static IRedisClientsManager _redisManager;
        
        public static Configure UseRedisTimeoutPersister(this Configure config, string endpointName, int defaultPollingTimeout = 10)
        {
            return config.UseRedisTimeoutPersister(endpointName, GetRedisClientsManager(), defaultPollingTimeout);
        }
        
        public static Configure UseRedisTimeoutPersister(this Configure config, string endpointName, IRedisClientsManager redisClientsManager, int defaultPollingTimeout = 10)
        {
            var redisTimeoutPersister = new RedisTimeoutPersistence(endpointName, redisClientsManager, defaultPollingTimeout);
            config.Configurer.RegisterSingleton<IPersistTimeouts>(redisTimeoutPersister);
            //config.Configurer.ConfigureComponent<RedisTimeoutPersistence>(DependencyLifecycle.SingleInstance);
            return config;
        }
        
        public static IRedisClientsManager GetRedisClientsManager()
        {
            var clusterNodes = ConfigurationManager.AppSettings["NServiceBus/Redis/RedisSentinelHosts"];
            var clusterName = ConfigurationManager.AppSettings["NServiceBus/Redis/RedisClusterName"];
            var redisConnectionString = ConfigurationManager.AppSettings["NServiceBus/Redis/RedisConnectionString"].Replace(";", "&");
            var redisManager = InitRedisClientManager(clusterName, clusterNodes, redisConnectionString);

            return redisManager;
        }

        internal static IRedisClientsManager InitRedisClientManager(string clusterName, string clusterNodes, string redisConnectionString)
        {
            lock (SyncObj)
            {
                if (_redisManager == null)
                {
                    if (string.IsNullOrWhiteSpace(clusterName) ||
                        string.IsNullOrWhiteSpace(clusterNodes))
                    {
                        _redisManager = new RedisManagerPool(redisConnectionString);
                    }
                    else
                    {
                        var sentinelHosts = clusterNodes.Split(',');

                        var sentinel = new RedisSentinel(sentinelHosts, clusterName);
                        sentinel.HostFilter = host => redisConnectionString.Fmt(host);
                        sentinel.RedisManagerFactory =
                            (master, slaves) => new PooledRedisClientManager(master, slaves);

                        _redisManager = sentinel.Start();
                    }
                }
            }

            return _redisManager;
        }
    }
}