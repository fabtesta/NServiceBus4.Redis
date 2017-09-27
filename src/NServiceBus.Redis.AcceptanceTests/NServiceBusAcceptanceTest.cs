using System;
using NServiceBus.AcceptanceTesting.Customization;

namespace NServiceBus.Redis.AcceptanceTests
{
    public class NServiceBusAcceptanceTest : IDisposable
    {
        
        public NServiceBusAcceptanceTest()
        {
            ////TEAR UP
            Conventions.EndpointNamingConvention = t =>
            {
                var baseNs = typeof(NServiceBusAcceptanceTest).Namespace;
                var testName = GetType().Name;
                return t.FullName.Replace(baseNs + ".", "").Replace(testName + "+", "")
                       + "." + System.Threading.Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(testName).Replace("_", "");
            };

            Conventions.DefaultRunDescriptor = () => ScenarioDescriptors.Transports.Default;
        }

        public void Dispose()
        {
            ////TEAR DOWN
        
        }
    }
}
