using System.Threading;

namespace Jering.Javascript.NodeJS
{
    /// <summary>
    /// <para>An abstraction for <see cref="Monitor"/>.</para>
    /// <para>This abstraction is necessary for testing of multi-threading logic in <see cref="OutOfProcessNodeJSService"/>.</para>
    /// </summary>
    public interface IMonitorService
    {
        /// <summary>
        /// Releases an exclusive lock on the specified object.
        /// </summary>
        /// <param name="obj">The object on which to release the lock.</param>
        void Exit(object obj);

        /// <summary>
        /// Attempts to acquire an exclusive lock on the specified object, and atomically sets a value that indicates whether the lock was taken.
        /// </summary>
        /// <param name="obj">The object on which to acquire the lock.</param>
        /// <param name="lockTaken">The result of the attempt to acquire the lock, passed by reference. The input must be false. The output is true if the lock is acquired; otherwise, the output is false. The output is 
        /// set even if an exception occurs during the attempt to acquire the lock.</param>
        void TryEnter(object obj, ref bool lockTaken);

        /// <summary>
        /// Acquires an exclusive lock on the specified object, and atomically sets a value that indicates whether the lock was taken.
        /// </summary>
        /// <param name="obj">The object on which to wait.</param>
        /// <param name="lockTaken">The result of the attempt to acquire the lock, passed by reference. The input must be false. The output is true if the lock is acquired; otherwise, the output is false. The output 
        /// is set even if an exception occurs during the attempt to acquire the lock. Note If no exception occurs, the output of this method is always true.</param>
        void Enter(object obj, ref bool lockTaken);

        /// <summary>
        /// Acquires an exclusive lock on the specified object.
        /// </summary>
        /// <param name="obj">The object on which to acquire the monitor lock.</param>
        void Enter(object obj);
    }
}
