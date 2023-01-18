using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Concurrent;
using System.Collections.Immutable;

namespace Jering.Javascript.NodeJS.CodeGenerators
{
    public static class RoslynExtensions
    {
        private static readonly ConcurrentDictionary<string, Func<AttributeListSyntax, bool>> _attributeListContainsAttributeNamePredicates = new();

        public static void AddEmbeddedResourceSource(ref IncrementalGeneratorPostInitializationContext context, string dotSeparatedDirectory, string fileName)
        {
            using Stream resourceStream = typeof(RoslynExtensions).Assembly.GetManifestResourceStream($"Jering.Javascript.NodeJS.Generators.{dotSeparatedDirectory}.{fileName}");
            var stringReader = new StreamReader(resourceStream);
            context.AddSource(fileName, stringReader.ReadToEnd());
        }

        public static bool SyntaxNodeHasAttribute(TypeDeclarationSyntax classDeclarationSyntax, string attributeName)
        {
            // We usually use [<attribute1>]\n[<attribute2>], but [<attribute1>, <attribute2>] is also acceptable. Hence AttributeListSyntax.
            SyntaxList<AttributeListSyntax> attributeLists = classDeclarationSyntax.AttributeLists;

            if (attributeLists.Count == 0)
            {
                return false;
            }

            return attributeLists.Any(GetAttributeListContainsAttributeNamePredicate(attributeName));
        }

        public static Func<AttributeListSyntax, bool> GetAttributeListContainsAttributeNamePredicate(string attributeName)
        {
            // Avoid creating predicate for every node
            if (_attributeListContainsAttributeNamePredicates.TryGetValue(attributeName, out Func<AttributeListSyntax, bool> predicate))
            {
                return predicate;
            }

            predicate = attributeList => attributeList.Attributes.Any(attribute => (attribute.Name as IdentifierNameSyntax)?.Identifier.ValueText == attributeName);
            _attributeListContainsAttributeNamePredicates.TryAdd(attributeName, predicate);

            return predicate;
        }

        public static IncrementalValuesProvider<T> WhereInitializedWithComparer<T>(this IncrementalValuesProvider<T> incrementalValuesProvider)
             where T : IInitializableData
        {
            return incrementalValuesProvider.Where(IsInitialized);
        }

        public static IncrementalValuesProvider<T> WhereUninitializedWithComparer<T>(this IncrementalValuesProvider<T> incrementalValuesProvider)
            where T : IInitializableData
        {
            return incrementalValuesProvider.Where(IsNotInitialized);
        }

        public static IncrementalValueProvider<T?> Single<T>(this IncrementalValuesProvider<T> incrementalValuesProvider)
        {
            return incrementalValuesProvider.Collect().Select(SelectFirstOrDefault);
        }

        public static IncrementalValueProvider<SortedList<T>> CollectSortedListWithComparer<T>(this IncrementalValuesProvider<T> incrementalValuesProvider)
        {
            return incrementalValuesProvider.Collect().Select(ImmutableArrayToSortedList<T>).WithComparer(new SortedListComparer<T>());
        }

        public static IncrementalValueProvider<SortedSet<T>> CollectSortedSetWithComparer<T>(this IncrementalValuesProvider<T> incrementalValuesProvider)
        {
            return incrementalValuesProvider.Collect().Select(ImmutableArrayToSortedSet<T>).WithComparer(new SortedSetComparer<T>());
        }

        public static IncrementalValuesProvider<T> Flatten<T>(this IncrementalValuesProvider<IEnumerable<T>> incrementalValuesProvider)
        {
            return incrementalValuesProvider.SelectMany(SelectUnchanged);
        }

        public static IncrementalValuesProvider<T> Flatten<T>(this IncrementalValueProvider<SortedSet<T>> incrementalValueProvider)
        {
            return incrementalValueProvider.SelectMany(SelectUnchanged);
        }

        public static IncrementalValuesProvider<T> Unique<T>(this IncrementalValueProvider<SortedList<T>> incrementalValueProvider)
        {
            return incrementalValueProvider.SelectMany(SelectUnique);
        }

        public static SortedList<T> ImmutableArrayToSortedList<T>(ImmutableArray<T> immutableArray, CancellationToken cancellationToken)
        {
            SortedList<T> result = new(immutableArray.Length);

            foreach (T element in immutableArray)
            {
                result.Add(element);
            }

            return result;
        }

        public static SortedSet<T> ImmutableArrayToSortedSet<T>(ImmutableArray<T> immutableArray, CancellationToken cancellationToken)
        {
            return new(immutableArray);
        }

        public static IEnumerable<T> SelectUnique<T>(SortedList<T> immutableArray, CancellationToken cancellationToken)
        {
            HashSet<T> result = new();

            foreach (T value in immutableArray)
            {
                result.Add(value);
            }

            return result;
        }

        public static T? SelectFirstOrDefault<T>(ImmutableArray<T> immutableArray, CancellationToken cancellationToken)
        {
            return immutableArray.FirstOrDefault();
        }

        public static T? GetNode<T>(GeneratorSyntaxContext context, CancellationToken _) where T : class
        {
            return context.Node as T;
        }

        public static bool ProgramClassDeclarationNodeSelector(SyntaxNode syntaxNode, CancellationToken _)
        {
            return syntaxNode is ClassDeclarationSyntax classDeclarationSyntax &&
                classDeclarationSyntax.Identifier.ValueText == "Program";
        }

        private static T SelectUnchanged<T>(T value, CancellationToken _)
        {
            return value;
        }

        private static bool IsInitialized<T>(T value) where T : IInitializableData
        {
            return value.Initialized;
        }

        private static bool IsNotInitialized<T>(T value) where T : IInitializableData
        {
            return !value.Initialized;
        }

        private class ImmutableArrayComparer<T> : IEqualityComparer<ImmutableArray<T>>
        {
            public bool Equals(ImmutableArray<T> x, ImmutableArray<T> y)
            {
                return x.SequenceEqual(y);
            }

            public int GetHashCode(ImmutableArray<T> obj)
            {
                return obj.GetHashCode();
            }
        }

        private class SortedListComparer<T> : IEqualityComparer<SortedList<T>>
        {
            public bool Equals(SortedList<T> x, SortedList<T> y)
            {
                return IEnumerableExtensions.SequenceEqualWithNullHandling(x, y);
            }

            public int GetHashCode(SortedList<T> obj)
            {
                return obj?.GetHashCode() ?? 0;
            }
        }

        private class SortedSetComparer<T> : IEqualityComparer<SortedSet<T>>
        {
            public bool Equals(SortedSet<T> x, SortedSet<T> y)
            {
                return IEnumerableExtensions.SequenceEqualWithNullHandling(x, y);
            }

            public int GetHashCode(SortedSet<T> obj)
            {
                return obj?.GetHashCode() ?? 0;
            }
        }
    }

    public interface IInitializableData
    {
        public bool Initialized { get; }
    }
}
