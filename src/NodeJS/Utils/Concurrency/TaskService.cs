using System;
using System.Threading.Tasks;

namespace Jering.Javascript.NodeJS
{
    /// <summary>
    /// Default implementation of <see cref="ITaskService"/>.
    /// </summary>
    public class TaskService : ITaskService
    {
        /// <inheritdoc />
        public Task Run(Action action)
        {
            return Task.Run(action);
        }

        /// <inheritdoc />
        public void WaitAll(params Task[] tasks)
        {
            Task.WaitAll(tasks);
        }

        /// <inheritdoc />
        public Task WhenAll(params Task[] tasks)
        {
            return Task.WhenAll(tasks);
        }
    }
}
