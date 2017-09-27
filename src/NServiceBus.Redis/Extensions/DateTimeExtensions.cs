using System;

namespace NServiceBus.Redis.Extensions
{
    internal static class DateTimeExtensions
    {
        internal static double ToUnixTimeSeconds(this DateTime time)
        {
            TimeSpan t = (time - new DateTime(1970, 1, 1));
            return t.TotalSeconds;
        }             
    }
}