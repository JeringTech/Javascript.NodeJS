namespace Jering.Javascript.NodeJS
{
    /// <summary>
    /// Source type of the module to be invoked in NodeJS.
    /// </summary>
    public enum ModuleSourceType
    {
        /// <summary>
        /// A module cached in NodeJS.
        /// </summary>
        Cache,

        /// <summary>
        /// A file.
        /// </summary>
        File,

        /// <summary>
        /// A string.
        /// </summary>
        String,

        /// <summary>
        /// A Stream.
        /// </summary>
        Stream
    }
}
