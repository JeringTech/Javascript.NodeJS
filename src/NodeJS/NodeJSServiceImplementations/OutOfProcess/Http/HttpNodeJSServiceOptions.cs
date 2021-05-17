using System;
using System.Net;

namespace Jering.Javascript.NodeJS
{
    /// <summary>
    /// Options for <see cref="HttpNodeJSService"/>s.
    /// </summary>
    public class HttpNodeJSServiceOptions
    {
#if NETCOREAPP3_1 || NET5_0
        /// <summary>The HTTP version to use.</summary>
        /// <remarks>
        /// <para>This value can be <see cref="HttpVersion.Version11"/> or <see cref="HttpVersion.Version20"/>. <see cref="HttpVersion.Version11"/> is faster than <see cref="HttpVersion.Version20"/>, 
        /// but <see cref="HttpVersion.Version20"/> may be more stable (unverified).</para>
        /// <para>If this value is not <see cref="HttpVersion.Version11"/> or <see cref="HttpVersion.Version20"/>, <see cref="HttpVersion.Version11"/> is used.</para>
        /// <para>This option is not available for the net461 and netstandard2.0 versions of this library because those framework versions do not support HTTP/2.0.</para>
        /// <para>Defaults to <see cref="HttpVersion.Version11"/>.</para>
        /// </remarks>
        public Version Version { get; set; } = HttpVersion.Version11;
#endif
    }
}
