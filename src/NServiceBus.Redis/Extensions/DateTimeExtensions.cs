using System;

namespace NServiceBus.Redis.Extensions
{
    public static class DateTimeExtensions
    {
        public static double ToUnixTimeSeconds(this DateTime time)
        {
            TimeSpan t = (time - new DateTime(1970, 1, 1));
            return t.TotalSeconds;
        }             
    }
}