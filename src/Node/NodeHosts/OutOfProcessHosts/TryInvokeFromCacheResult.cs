namespace Jering.JavascriptUtils.Node.NodeHosts.OutOfProcessHosts
{
    public class TryInvokeFromCacheResult<T>
    {
        public T Value { get; }
        public bool CacheMiss { get; }
    }
}
