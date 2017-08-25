using System.Configuration;
using NServiceBus.Logging;
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
        
        static readonly ILog Logger = LogManager.GetLogger(typeof(ConfigureRedisPersistenceManager));                

        public static Configure UseRedisTimeoutPersister(this Configure config, string endpointName, int defaultPollingTimeout = 10)
        {
            Logger.InfoFormat("ConfigureRedisPersistenceManager UseRedisTimeoutPersister endpointName {0} defaultPollingTimeout {1}", endpointName,  defaultPollingTimeout);
            return config.UseRedisTimeoutPersister(endpointName, GetRedisClientsManager(), defaultPollingTimeout);
        }
        
        public static Configure UseRedisTimeoutPersister(this Configure config, string endpointName, IRedisClientsManager redisClientsManager, int defaultPollingTimeout = 10)
        {
            var redisTimeoutPersister = new RedisTimeoutPersistence(endpointName, redisClientsManager, defaultPollingTimeout);
            config.Configurer.RegisterSingleton<IPersistTimeouts>(redisTimeoutPersister);
            //config.Configurer.ConfigureComponent<RedisTimeoutPersistence>(DependencyLifecycle.SingleInstance);
            Logger.InfoFormat("ConfigureRedisPersistenceManager UseRedisTimeoutPersister endpointName {0} defaultPollingTimeout {1} redisClientsManager {2}", endpointName,  defaultPollingTimeout, redisClientsManager.GetType().FullName);
            return config;
        }
        
        public static IRedisClientsManager GetRedisClientsManager()
        {
            var clusterNodes = ConfigurationManager.AppSettings["NServiceBus/Redis/RedisSentinelHosts"];
            var clusterName = ConfigurationManager.AppSettings["NServiceBus/Redis/RedisClusterName"];
            var redisConnectionString = ConfigurationManager.AppSettings["NServiceBus/Redis/RedisConnectionString"].Replace(";", "&");
            var redisManager = InitRedisClientManager(clusterName, clusterNodes, redisConnectionString);

            var serviceStackLicense = ConfigurationManager.AppSettings["servicestack:license"] != null ? "found" : "not found (missing servicestack:license key)";
            Logger.InfoFormat("ConfigureRedisPersistenceManager GetRedisClientsManager clusterNodes {0} clusterName {1} redisConnectionString {2} servicestack licence {3}", clusterNodes,  clusterName, redisConnectionString, serviceStackLicense);
            
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