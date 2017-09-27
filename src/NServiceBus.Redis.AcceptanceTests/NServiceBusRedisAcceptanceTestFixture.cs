using System;
using NServiceBus.AcceptanceTesting.Customization;

namespace NServiceBus.Redis.AcceptanceTests
{
    public class NServiceBusRedisAcceptanceTestFixture : IDisposable
    {
        
        public NServiceBusRedisAcceptanceTestFixture()
        {
            ////TEAR UP
            Conventions.EndpointNamingConvention = t =>
            {
                var baseNs = typeof(NServiceBusRedisAcceptanceTestFixture).Namespace;
                var testName = GetType().Name;
                return t.FullName.Replace(baseNs + ".", "").Replace(testName + "+", "")
                       + "." + System.Threading.Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(testName).Replace("_", "");
            };            
        }

        public void Dispose()
        {
            ////TEAR DOWN
        
        }
    }
}
