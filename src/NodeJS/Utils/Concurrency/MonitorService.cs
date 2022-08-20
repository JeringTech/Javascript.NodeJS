using System.Threading;

namespace Jering.Javascript.NodeJS
{
    /// <summary>
    /// Default implementation of <see cref="IMonitorService"/>.
    /// </summary>
    public class MonitorService : IMonitorService
    {
        /// <inheritdoc />
        public void TryEnter(object obj, ref bool lockTaken)
        {
            Monitor.TryEnter(obj, ref lockTaken);
        }

        /// <inheritdoc />
        public void Exit(object obj)
        {
            Monitor.Exit(obj);
        }

        /// <inheritdoc />
        public void Enter(object obj, ref bool lockTaken)
        {
            Monitor.Enter(obj, ref lockTaken);
        }

        /// <inheritdoc />
        public void Enter(object obj)
        {
            Monitor.Enter(obj);
        }
    }
}
