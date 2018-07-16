using System;

namespace Jering.JavascriptUtils.Node
{
    /// <summary>
    /// Result for <see cref="INodeJSService.TryInvokeFromCacheAsync{T}"/>.
    /// </summary>
    /// <typeparam name="T">Type of the expected return value.</typeparam>
    public class InvocationResult<T>
    {
        public InvocationResult(T value, bool cacheMiss)
        {
            Value = value;
            CacheMiss = cacheMiss;
        }

        public T Value { get; }
        public bool CacheMiss { get; }
    }
}
