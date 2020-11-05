namespace Weikio.ApiFramework.Plugins.OpenApi
{
    /// <summary>
    /// Defines the default tag transform for proxies Open Apis.
    /// Defaults to <see cref="UseOriginal"/>.
    /// To fully customize tag transform, <see cref="ApiOptions.TransformTags"/>
    /// </summary>
    public enum TagTransformModeEnum
    {
        /// <summary>
        /// Use original tags
        /// </summary>
        UseOriginal,
        
        /// <summary>
        /// Add endpoint's name (if exists) or route to the original tag collection
        /// </summary>
        AddEndpointNameOrRoute,
        
        /// <summary>
        /// Replace original tags with endpoint's name (if exists) or route to the original tag collection 
        /// </summary>
        UseEndpointNameOrRoute
    }
}
