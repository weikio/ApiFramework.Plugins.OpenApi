using System;

namespace Weikio.ApiFramework.Plugins.OpenApi
{
    public class ApiOptions
    {
        public string SpecificationUrl { get; set; }

        public AuthenticationOptions Authentication { get; set; }

        public HttpClientOptions HttpClient { get; set; } = new HttpClientOptions();
    }

    public class AuthenticationOptions
    {
        public string Basic { get; set; }

        public string Bearer { get; set; }
    }

    public class HttpClientOptions
    {
        public TimeSpan? SlidingExpiration { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Request timeout. Use standard TimeSpan string format.
        /// </summary>
        public string Timeout { get; set; }
    }
}
