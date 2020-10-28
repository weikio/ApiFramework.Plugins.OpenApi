using Newtonsoft.Json.Linq;

namespace Weikio.ApiFramework.Plugins.OpenApi
{
    public class ServerUrl
    {
        public string Url { get; set; }

        public ServerUrl(string url)
        {
            Url = url;
        }
    }
}
