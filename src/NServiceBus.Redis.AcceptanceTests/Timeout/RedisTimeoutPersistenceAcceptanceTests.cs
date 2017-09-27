using System;
using NServiceBus.AcceptanceTesting;
using NServiceBus.Redis.AcceptanceTests.EndpointTemplates;
using Xunit;

namespace NServiceBus.Redis.AcceptanceTests.Timeout
{
    [Trait("Category", "Integration")]
    [Collection("NServiceBusRedisAcceptanceTestFixture")]
    public class RedisTimeoutPersistenceAcceptanceTests
    {
        [Fact(Skip = "context scenario still not working")]
        public void Message_should_be_received()
        {
            var context = new Context();

            Scenario.Define(context)
                    .WithEndpoint<Endpoint>(b => b.Given((bus, c) => bus.Defer(TimeSpan.FromSeconds(3), new MyMessage())))
                    .Done(c => c.WasCalled)
                    .Run();

            Assert.True(context.WasCalled);
        }

        public class Context : ScenarioContext
        {
            public bool WasCalled { get; set; }
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>();
            }
            public class MyMessageHandler : IHandleMessages<MyMessage>
            {
                public Context Context { get; set; }

                public IBus Bus { get; set; }

                public void Handle(MyMessage message)
                {
                    Context.WasCalled = true;
                }
            }
        }        
    }
}
