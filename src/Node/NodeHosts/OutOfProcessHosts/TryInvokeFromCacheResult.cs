namespace Jering.JavascriptUtils.Node.NodeHosts.OutOfProcessHosts
{
    /// <summary>
    /// Result for <see cref="INodeHost.TryInvokeFromCacheAsync{T}"/>.
    /// </summary>
    /// <typeparam name="T">Type of the expected return value.</typeparam>
    public class TryInvokeFromCacheResult<T>
    {
        public T Value { get; }
        public bool CacheMiss { get; }
    }
}
