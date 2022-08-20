using System;
using System.Threading.Tasks;

namespace Jering.Javascript.NodeJS
{
    /// <summary>
    /// An abstraction for <see cref="Task"/>.
    /// </summary>
    public interface ITaskService
    {
        /// <summary>
        /// Queues the specified work to run on the thread pool and returns a Task object that represents that work.
        /// </summary>
        /// <param name="action">The work to execute asynchronously.</param>
        Task Run(Action action);

        /// <summary>
        /// Waits for all of the provided Task objects to complete execution.
        /// </summary>
        /// <param name="tasks">An array of Task instances on which to wait.</param>
        void WaitAll(params Task[] tasks);
    }
}
