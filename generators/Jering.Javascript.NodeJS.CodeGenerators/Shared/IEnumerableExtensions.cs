namespace Jering.Javascript.NodeJS.CodeGenerators
{
    public static class IEnumerableExtensions
    {
        public static bool SequenceEqualWithNullHandling<T>(IEnumerable<T>? first, IEnumerable<T>? second)
        {
            if (first == second)
            {
                return true;
            }

            if (first == null || second == null)
            {
                return false;
            }

            return first.SequenceEqual(second);
        }
    }
}
