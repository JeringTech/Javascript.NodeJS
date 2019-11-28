using System;

namespace Jering.Javascript.NodeJS
{
    /// <summary>
    /// The default implementation of <see cref="IEnvironmentService"/>.
    /// </summary>
    public class EnvironmentService : IEnvironmentService
    {
        /// <inheritdoc />
        public int ProcessorCount
        {
            get
            {
                return Environment.ProcessorCount;
            }
        }
    }
}
