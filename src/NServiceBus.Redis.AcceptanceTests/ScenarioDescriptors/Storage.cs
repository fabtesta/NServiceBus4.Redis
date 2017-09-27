using System;
using System.Collections.Generic;
using System.Linq;
using NServiceBus.AcceptanceTesting.Support;
using NServiceBus.Redis.Timeout;
using NServiceBus.Transports;

namespace NServiceBus.Redis.AcceptanceTests.ScenarioDescriptors
{
    public class Storage : ScenarioDescriptor
    {
        public static IEnumerable<RunDescriptor> AllAvailable => _availableTransports ?? (_availableTransports = GetAllAvailable().ToList());

        public static RunDescriptor Redis
        {
            get { return AllAvailable.SingleOrDefault(r => r.Key == "Redis"); }
        }

        static IEnumerable<RunDescriptor> GetAllAvailable()
        {
            var foundTransportDefinitions = TypeScanner.GetAllTypesAssignableTo<TransportDefinition>();

            foreach (var transportDefinitionType in foundTransportDefinitions)
            {
                var key = transportDefinitionType.Name;

                var runDescriptor = new RunDescriptor
                {
                    Key = key,
                    Settings =
                        new Dictionary<string, string>
                                {
                                    {"Transport", transportDefinitionType.AssemblyQualifiedName}
                                }
                };

                var connectionString = Environment.GetEnvironmentVariable(key + ".ConnectionString");

                if (string.IsNullOrEmpty(connectionString) && DefaultConnectionStrings.ContainsKey(key))
                    connectionString = DefaultConnectionStrings[key];


                if (!string.IsNullOrEmpty(connectionString))
                {
                    runDescriptor.Settings.Add("NServiceBus/Redis/RedisSentinelHosts", connectionString);
                    yield return runDescriptor;
                }
                else
                {
                    Console.WriteLine("No connection string found for transport: {0}, test will not be executed for this transport {1}", key, "");
                }
            }
        }

        static IList<RunDescriptor> _availableTransports;

        static readonly Dictionary<string, string> DefaultConnectionStrings = new Dictionary<string, string>
            {
                {"RabbitMQ", "host=localhost"}
            };
    }
}