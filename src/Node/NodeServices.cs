using System;
using System.Threading;
using System.Threading.Tasks;
using Jering.JavascriptUtils.Node.HostingModels;

namespace Jering.JavascriptUtils.Node
{
    /// <summary>
    /// <para>
    /// Default implementation of INodeServices. This is the primary API surface through which developers
    /// make use of this package. It provides simple "InvokeAsync" methods that dispatch calls to the
    /// correct Node instance, creating and destroying those instances as needed.
    /// </para>
    /// <para>
    /// If a Node instance dies (or none was yet created), this class takes care of creating a new one.
    /// If a Node instance signals that it needs to be restarted (e.g., because a file changed), then this
    /// class will create a new instance and dispatch future calls to it, while keeping the old instance
    /// alive for a defined period so that any in-flight RPC calls can complete. This latter feature is
    /// analogous to the "connection draining" feature implemented by HTTP load balancers.
    /// </para>
    /// </summary>
    /// <seealso cref="INodeServices" />
    internal class NodeServices : INodeServices
    {
        private static readonly TimeSpan _connectionDrainingTimespan = TimeSpan.FromSeconds(15);
        private readonly INodeInstanceFactory _nodeInstanceFactory;
        private INodeInstance _currentNodeInstance;
        private readonly object _currentNodeInstanceAccessLock = new object();
        private Exception _instanceDelayedDisposalException;

        internal NodeServices(INodeInstanceFactory nodeInstanceFactory)
        {
            _nodeInstanceFactory = nodeInstanceFactory;
        }

        public Task<T> InvokeAsync<T>(string moduleName, object[] args = null, string exportedFunctionName = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return InvokeCoreAsync<T>(moduleName, exportedFunctionName, args, true, false, cancellationToken);
        }

        public Task<T> InvokeFromMemoryAsync<T>(string module, object[] args = null, string exportedFunctionName = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return InvokeCoreAsync<T>(module, exportedFunctionName, args, true, true, cancellationToken);
        }

        private async Task<T> InvokeCoreAsync<T>(string asset, string exportedFunctionName, object[] args, bool allowRetry, bool fromMemory, CancellationToken cancellationToken)
        {
            ThrowAnyOutstandingDelayedDisposalException();
            INodeInstance nodeInstance = GetOrCreateCurrentNodeInstance();

            try
            {
                // TODO if from memory vs module

                return await nodeInstance.InvokeExportAsync<T>(cancellationToken, asset, exportedFunctionName, args).ConfigureAwait(false);
            }
            catch (NodeInvocationException ex)
            {
                // If the Node instance can't complete the invocation because it needs to restart (e.g., because the underlying
                // Node process has exited, or a file it depends on has changed), then we make one attempt to restart transparently.
                if (allowRetry && ex.NodeInstanceUnavailable)
                {
                    // Perform the retry after clearing away the old instance
                    // Since we disposal is delayed even though the node instance is replaced immediately, this produces the
                    // "connection draining" feature whereby in-flight RPC calls are given a certain period to complete.
                    lock (_currentNodeInstanceAccessLock)
                    {
                        if (_currentNodeInstance == nodeInstance)
                        {
                            TimeSpan disposalDelay = ex.AllowConnectionDraining ? _connectionDrainingTimespan : TimeSpan.Zero;
                            DisposeNodeInstance(_currentNodeInstance, disposalDelay);
                            _currentNodeInstance = null;
                        }
                    }

                    // One the next call, don't allow retries, because we could get into an infinite retry loop, or a long retry
                    // loop that masks an underlying problem. A newly-created Node instance should be able to accept invocations,
                    // or something more serious must be wrong.
                    return await InvokeCoreAsync<T>(asset, exportedFunctionName, args, false, fromMemory, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    throw;
                }
            }
        }

        public void Dispose()
        {
            lock (_currentNodeInstanceAccessLock)
            {
                if (_currentNodeInstance != null)
                {
                    DisposeNodeInstance(_currentNodeInstance, delay: TimeSpan.Zero);
                    _currentNodeInstance = null;
                }
            }
        }

        private void DisposeNodeInstance(INodeInstance nodeInstance, TimeSpan delay)
        {
            if (delay == TimeSpan.Zero)
            {
                nodeInstance.Dispose();
            }
            else
            {
                Task.Run(async () => {
                    try
                    {
                        await Task.Delay(delay).ConfigureAwait(false);
                        nodeInstance.Dispose();
                    }
                    catch(Exception ex)
                    {
                        // Nothing's waiting for the delayed disposal task, so any exceptions in it would
                        // by default just get ignored. To make these discoverable, capture them here so
                        // they can be rethrown to the next caller to InvokeExportAsync.
                        _instanceDelayedDisposalException = ex;
                    }
                });
            }
        }

        private void ThrowAnyOutstandingDelayedDisposalException()
        {
            if (_instanceDelayedDisposalException != null)
            {
                Exception ex = _instanceDelayedDisposalException;
                _instanceDelayedDisposalException = null;
                throw new AggregateException(
                    "A previous attempt to dispose a Node instance failed. See InnerException for details.",
                    ex);
            }
        }

        private INodeInstance GetOrCreateCurrentNodeInstance()
        {
            INodeInstance instance = _currentNodeInstance;
            if (instance == null)
            {
                lock (_currentNodeInstanceAccessLock)
                {
                    instance = _currentNodeInstance ?? (_currentNodeInstance = CreateNewNodeInstance());
                }
            }

            return instance;
        }

        private INodeInstance CreateNewNodeInstance()
        {
            return _nodeInstanceFactory.Create();
        }
    }
}