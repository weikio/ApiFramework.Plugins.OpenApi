using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;

namespace Weikio.ApiFramework.Plugins.OpenApi
{
    internal static class HttpClientFactory
    {
        private static readonly MemoryCache _cache = new MemoryCache(new MemoryCacheOptions());

        public static HttpClient CreateClient(ApiOptions options)
        {
            var cacheKey = GetCacheKey(options);

            return _cache.GetOrCreate(cacheKey, entry =>
            {
                if (options.HttpClient?.SlidingExpiration.HasValue == true)
                {
                    entry.SetSlidingExpiration(options.HttpClient.SlidingExpiration.Value);
                }

                var client = new HttpClient();

                if (TimeSpan.TryParse(options.HttpClient?.Timeout, out var timeout))
                {
                    client.Timeout = timeout;
                }

                ConfigureAuthentication(client, options);

                return client;
            });
        }

        private static void ConfigureAuthentication(HttpClient client, ApiOptions apiOptions)
        {
            if (apiOptions.Authentication == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(apiOptions.Authentication.Basic))
            {
                var base64Credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes(apiOptions.Authentication.Basic));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", base64Credentials);
            }
            else if (!string.IsNullOrEmpty(apiOptions.Authentication.Bearer))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiOptions.Authentication.Bearer);
            }
        }

        private static long GetCacheKey(ApiOptions options)
        {
            return options.GetHashCode() + JsonConvert.SerializeObject(options).GetHashCode();
        }
    }
}
