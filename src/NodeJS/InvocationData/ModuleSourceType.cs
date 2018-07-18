namespace Jering.JavascriptUtils.NodeJS
{
    /// <summary>
    /// Source type of modules to be invoked in Node.js.
    /// </summary>
    public enum ModuleSourceType
    {
        /// <summary>
        /// Cached in Node.js.
        /// </summary>
        Cache,

        File,
        String,
        Stream
    }
}
