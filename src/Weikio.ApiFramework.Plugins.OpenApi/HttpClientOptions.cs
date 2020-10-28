using System;

namespace Weikio.ApiFramework.Plugins.OpenApi
{
    public class HttpClientOptions
    {
        public TimeSpan? SlidingExpiration { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Request timeout. Use standard TimeSpan string format.
        /// </summary>
        public string Timeout { get; set; }
    }
}