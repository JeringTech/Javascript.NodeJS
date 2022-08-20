using System;

namespace Jering.Javascript.NodeJS
{
    /// <summary>
    /// An abstraction for <see cref="Environment"/>
    /// </summary>
    public interface IEnvironmentService
    {
        /// <summary>
        /// Gets the number of processors on the current machine.
        /// </summary>
        int ProcessorCount { get; }
    }
}
