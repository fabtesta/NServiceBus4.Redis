using System;
using System.Collections.Generic;

namespace NServiceBus.Redis.AcceptanceTests.EndpointTemplates
{
    public static class ConfigureExtensions
    {
        public static string GetOrNull(this IDictionary<string, string> dictionary, string key)
        {
            if (!dictionary.ContainsKey(key))
            {
                return null;
            }

            return dictionary[key];
        }

        public static Configure DefineStorage(this Configure config)
        {
            var settings = ScenarioDescriptors.Storage.RabbitMq.Settings;           
            var transportType = Type.GetType(settings["Transport"]);

            return config.UseTransport(transportType, () => settings["Transport.ConnectionString"]);
        }
        public static Configure DefineTransport(this Configure config)
        {
            var settings = ScenarioDescriptors.Transports.RabbitMq.Settings;           
            var transportType = Type.GetType(settings["Transport"]);

            return config.UseTransport(transportType, () => settings["Transport.ConnectionString"]);
        }

        public static Configure DefineSerializer(this Configure config)
        {
            Configure.Serialization.Json();
            return config;
        }

        public static Configure DefineTimeoutPersister(this Configure config, string endpointName)
        {
            return config.UseRedisTimeoutPersister(endpointName);
        }

        public static Configure DefineTimeoutPersister(this Configure config, string endpointName)
        {
            return config.UseRedisGatewayDeduplicationStorage(endpointName);
        }
    }
}