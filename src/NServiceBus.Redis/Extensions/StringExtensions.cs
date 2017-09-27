namespace NServiceBus.Redis.Extensions
{
    internal static class StringExtensions
    {
        internal static string EscapeClientId(this string clientId)
        {
            return clientId.Replace("\\", "_");
        }
    }
}
