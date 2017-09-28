using System.Collections.Generic;

namespace NServiceBus.Redis.Gateway
{
    internal class GatewayMessage : NServiceBus.Gateway.Deduplication.GatewayMessage
    {
        public IDictionary<string, string> Headers { get; set; }

        public byte[] OriginalMessage { get; set; }

        public bool Acknowledged { get; set; }
    }
}
