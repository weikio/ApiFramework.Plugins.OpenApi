namespace Weikio.ApiFramework.Plugins.OpenApi
{
    /// <summary>
    /// Defines the default tag transform for proxies Open Apis.
    /// Defaults to <see cref="AutoPrefixWithRouteOrCustomPrefix"/>.
    /// </summary>
    public enum PrefixMode
    {
        /// <summary>
        /// Prefix with route or with ApiOptions.Prefix if set
        /// </summary>
        AutoPrefixWithRouteOrCustomPrefix,
        
        /// <summary>
        /// Prefix with ApiOptions.Prefix if set, otherwise no prefix
        /// </summary>
        OnlyPrefix
    }
}
