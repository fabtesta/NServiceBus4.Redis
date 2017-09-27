using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NServiceBus.Redis.Gateway
{
    internal class GatewayEntity
    {
        public IDictionary<string, string> Headers { get; set; }

        public DateTime TimeReceived { get; set; }

        public string Id { get; set; }

        public byte[] OriginalMessage { get; set; }

        public bool Acknowledged { get; set; }
    }
}
