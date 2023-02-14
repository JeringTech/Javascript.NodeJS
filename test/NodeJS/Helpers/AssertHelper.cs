using System.Diagnostics.CodeAnalysis;
using Xunit;

namespace Jering.Javascript.NodeJS.Tests
{
    public static class AssertHelper
    {
        public static void NotNull([NotNull] object? obj)
        {
            Assert.NotNull(obj);
        }
    }
}
