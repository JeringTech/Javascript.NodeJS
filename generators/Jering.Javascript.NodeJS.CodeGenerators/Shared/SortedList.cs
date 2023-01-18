using System.Collections;

namespace Jering.Javascript.NodeJS.CodeGenerators
{
    public class SortedList<T> : ICollection<T>
    {
        private readonly SortedList<T, T> _innerList;

        public int Count => _innerList.Count;

        public bool IsReadOnly => false;

        public SortedList()
        {
            _innerList = new();
        }

        public SortedList(int capacity)
        {
            _innerList = new(capacity);
        }

        public SortedList(IComparer<T> comparer)
        {
            _innerList = new(comparer);
        }

        public void Add(T element)
        {
            _innerList.Add(element, element);
        }

        public void Clear()
        {
            _innerList.Clear();
        }

        public bool Contains(T item)
        {
            return _innerList.Contains(new KeyValuePair<T, T>(item, item));
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            foreach (KeyValuePair<T, T> keyValuePair in _innerList)
            {
                array[arrayIndex++] = keyValuePair.Key;
            }
        }

        public bool Remove(T item)
        {
            return _innerList.Remove(item);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return new SortedListEnumerator(_innerList);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public override bool Equals(object? obj)
        {
            return obj is SortedList<T> list && EqualityComparer<SortedList<T, T>>.Default.Equals(_innerList, list._innerList);
        }

        public override int GetHashCode()
        {
            return 1295128890 + EqualityComparer<SortedList<T, T>>.Default.GetHashCode(_innerList);
        }

        public struct SortedListEnumerator : IEnumerator<T>
        {
            private readonly IEnumerator<KeyValuePair<T, T>> _innerListEnumerator;

            public T Current { get; private set; }

            public SortedListEnumerator(SortedList<T, T> innerList)
            {
                _innerListEnumerator = innerList.GetEnumerator();
                Current = default;
            }

            public bool MoveNext()
            {
                if (!_innerListEnumerator.MoveNext())
                {
                    return false;
                }

                Current = _innerListEnumerator.Current.Key;
                return true;
            }

            public void Reset()
            {
                // We're only enumerating in foreach loops
                throw new NotImplementedException();
            }

            public void Dispose()
            {
                // Nothing to dispose
            }

            // Legacy
            object IEnumerator.Current => Current!;
        }
    }
}
