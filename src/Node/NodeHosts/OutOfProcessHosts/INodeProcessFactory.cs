using System.Diagnostics;

namespace Jering.JavascriptUtils.Node.Node.OutOfProcessHosts
{
    public interface INodeProcessFactory
    {
        Process Create(string nodeServerScript);
    }
}
