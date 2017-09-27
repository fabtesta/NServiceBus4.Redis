using System;
using System.Configuration;
using NServiceBus.Gateway.Deduplication;
using NServiceBus.Gateway.Persistence;
using NServiceBus.Logging;
using NServiceBus.Redis.Gateway;
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

        /// <summary>
        /// Configures default redisclientsmanager.
        /// </summary>
        public static Configure RedisStorage(this Configure config)
        {
            Logger.InfoFormat(
                "RedisStorage default");
            return config.RedisStorage(GetRedisClientsManager());
        }

        /// <summary>
        /// Configures your own instance of redisclientsmanager.
        /// </summary>
        public static Configure RedisStorage(this Configure config, IRedisClientsManager redisClientsManager)
        {
            Logger.InfoFormat(
                "RedisStorage {0}", redisClientsManager.GetType().FullName);
            config.Configurer.RegisterSingleton<IRedisClientsManager>(redisClientsManager);
            return config;
        }

        /// <summary>
        /// Use Redis timeout persistence.
        /// </summary>
        public static Configure UseRedisTimeoutPersister(this Configure config, string endpointName,
            int defaultPollingTimeout = 10)
        {
            config.ThrowIfRedisNotConfigured();

            config.Configurer.RegisterSingleton<IPersistTimeouts>(typeof(RedisTimeoutPersistence))
                .ConfigureProperty<string>("endpointName", endpointName)
                .ConfigureProperty<int>("defaultPollingTimeout", defaultPollingTimeout);
            Logger.InfoFormat(
                "ConfigureRedisPersistenceManager UseRedisTimeoutPersister endpointName {0} defaultPollingTimeout {1}",
                endpointName, defaultPollingTimeout);
            return config;
        }

        /// <summary>
        /// Use Redis messages persistence by the gateway.
        /// </summary>
        public static Configure UseRedisGatewayStorage(this Configure config, string endpointName,
            int defaultEntityTtl = 10)
        {
            config.ThrowIfRedisNotConfigured();

            config.Configurer.RegisterSingleton<IPersistMessages>(typeof(RedisGatewayPersistence))
                .ConfigureProperty<string>("endpointName", endpointName)
                .ConfigureProperty<int>("defaultEntityTtl", defaultEntityTtl);
            Logger.InfoFormat(
                "ConfigureRedisPersistenceManager UseRedisGatewayStorage endpointName {0} defaultEntityTtl {1}",
                endpointName, defaultEntityTtl);
            return config.RunGateway(typeof(RedisGatewayPersistence));
        }

        /// <summary>
        /// Use Redis for message deduplication by the gateway.
        /// </summary>
        public static Configure UseRedisGatewayDeduplicationStorage(this Configure config, string endpointName,
            int defaultEntityTtl = 10)
        {
            config.ThrowIfRedisNotConfigured();

            config.Configurer.RegisterSingleton<IDeduplicateMessages>(typeof(RedisDeduplication))
                .ConfigureProperty<string>("endpointName", endpointName)
                .ConfigureProperty<int>("defaultEntityTtl", defaultEntityTtl);
            Logger.InfoFormat(
                "ConfigureRedisPersistenceManager UseRedisGatewayDeduplicationStorage endpointName {0} defaultEntityTtl {1}",
                endpointName, defaultEntityTtl);            
            return config;
        }


        public static IRedisClientsManager GetRedisClientsManager()
        {
            var clusterNodes = ConfigurationManager.AppSettings["NServiceBus/Redis/RedisSentinelHosts"];
            if(clusterNodes == null)
                throw new Exception("Missing NServiceBus/Redis/RedisSentinelHosts appSettings key (leave it empty for single node connection pool).");

            var clusterName = ConfigurationManager.AppSettings["NServiceBus/Redis/RedisClusterName"];
            if (clusterName == null)
                throw new Exception("Missing NServiceBus/Redis/RedisClusterName appSettings key (leave it empty for single node connection pool).");

            var redisConnectionString = ConfigurationManager.AppSettings["NServiceBus/Redis/RedisConnectionString"];
            if (redisConnectionString == null)
                throw new Exception("Missing NServiceBus/Redis/RedisConnectionString appSettings key.");

            redisConnectionString = redisConnectionString.Replace(";", "&");
            var redisManager = InitRedisClientManager(clusterName, clusterNodes, redisConnectionString);

            var serviceStackLicense = ConfigurationManager.AppSettings["servicestack:license"] != null
                ? "found"
                : "not found (missing servicestack:license key)";
            Logger.InfoFormat(
                "ConfigureRedisPersistenceManager GetRedisClientsManager clusterNodes {0} clusterName {1} redisConnectionString {2} servicestack licence {3}",
                clusterNodes, clusterName, redisConnectionString, serviceStackLicense);

            return redisManager;
        }

        internal static IRedisClientsManager InitRedisClientManager(string clusterName, string clusterNodes,
            string redisConnectionString)
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

        internal static void ThrowIfRedisNotConfigured(this Configure config)
        {
            if (!config.Configurer.HasComponent<IRedisClientsManager>())
            {
                throw new Exception("Call config.RedisStorage() first.");
            }
        }
    }
}