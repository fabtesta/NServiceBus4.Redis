using System;
using System.Collections.Generic;

namespace NServiceBus.Redis.Timeout
{    
    internal class TimeoutEntity
    {
        public string Id { get; set; }
        public string Destination { get; set; }
        public Guid SagaId { get; set; }
        public byte[] State { get; set; }
        public DateTime Time { get; set; }
        public string TimeString { get; set; }
        public Dictionary<string, string> Headers { get; set; }
        public string CorrelationId { get; set; }
        public string OwningTimeoutManager { get; set; }
    }
}
