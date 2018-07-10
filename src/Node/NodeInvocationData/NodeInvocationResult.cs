namespace Jering.JavascriptUtils.Node
{
    /// <summary>
    /// Result for <see cref="INodeService.TryInvokeFromCacheAsync{T}"/>.
    /// </summary>
    /// <typeparam name="T">Type of the expected return value.</typeparam>
    public class NodeInvocationResult<T>
    {
        public T Value { get; }
        public bool CacheMiss { get; }
    }
}
