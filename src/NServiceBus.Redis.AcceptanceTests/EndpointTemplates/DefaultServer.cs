using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NServiceBus.AcceptanceTesting.Support;
using NServiceBus.Config.ConfigurationSource;
using NServiceBus.Hosting.Helpers;
using NServiceBus.Settings;

namespace NServiceBus.Redis.AcceptanceTests.EndpointTemplates
{
    public class DefaultServer : IEndpointSetupTemplate
    {
        public Configure GetConfiguration(RunDescriptor runDescriptor, EndpointConfiguration endpointConfiguration, IConfigurationSource configSource)
        {
            var types = GetTypesToUse(endpointConfiguration);

            Configure.Features.Disable<Features.Sagas>();
            Configure.Features.Enable<Features.Gateway>();
            Configure.Features.Enable<Features.TimeoutManager>();
            Configure.Serialization.Json();

            SettingsHolder.SetDefault("ScaleOut.UseSingleBrokerQueue", true);

            var config = Configure.With(types)
                            .DefineEndpointName(endpointConfiguration.EndpointName)
                            //.CustomConfigurationSource(configSource)
                            .RedisStorage()
                            .UseTransport<RabbitMQ>()
                            .UseRedisGatewayDeduplicationStorage(endpointConfiguration.EndpointName)
                            .UseRedisTimeoutPersister(endpointConfiguration.EndpointName)
                            .RunGateway().UseRedisGatewayStorage(endpointConfiguration.EndpointName);
            return config.UnicastBus();
        }

        static IEnumerable<Type> GetTypesToUse(EndpointConfiguration endpointConfiguration)
        {
            var assemblies = new AssemblyScanner().GetScannableAssemblies();

            var types = assemblies.Assemblies
                //exclude all test types by default
                                  .Where(a => a != Assembly.GetExecutingAssembly())
                                  .SelectMany(a => a.GetTypes());

            types = types.Union(GetNestedTypeRecursive(endpointConfiguration.BuilderType.DeclaringType, endpointConfiguration.BuilderType));

            types = types.Union(endpointConfiguration.TypesToInclude);

            return types.Where(t => !endpointConfiguration.TypesToExclude.Contains(t)).ToList();
        }

        static IEnumerable<Type> GetNestedTypeRecursive(Type rootType, Type builderType)
        {
            yield return rootType;

            if (typeof(IEndpointConfigurationFactory).IsAssignableFrom(rootType) && rootType != builderType)
                yield break;

            foreach (var nestedType in rootType.GetNestedTypes(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).SelectMany(t => GetNestedTypeRecursive(t, builderType)))
            {
                yield return nestedType;
            }
        }

    }
}