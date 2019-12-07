using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Jering.Javascript.NodeJS.Performance
{
    // Benchmarks for threading methods considered for HttpNodeJSPoolServices
    [MemoryDiagnoser]
    public class PoolThreadingBenchmarks
    {
        private LockFreeMethod _lockFree;
        private LockMethod _lock;

        private IEnumerable<int> _source = Enumerable.Range(0, 2000);

        [GlobalSetup]
        public void Setup()
        {
            _lock = new LockMethod();
            _lockFree = new LockFreeMethod();

            // Keep benchmarks consistent
            ThreadPool.SetMinThreads(Environment.ProcessorCount, Environment.ProcessorCount);
            ThreadPool.SetMaxThreads(Environment.ProcessorCount, Environment.ProcessorCount);
        }

        [Benchmark]
        public void Lock()
        {
            _lock.GetDummyObject();
        }

        [Benchmark]
        public void LockFree()
        {
            _lockFree.GetDummyObject();
        }

        [Benchmark]
        public void LockFree_Parallel()
        {
            Parallel.ForEach(_source, LockFree_GetDummyObject);
        }

        private void LockFree_GetDummyObject(int _)
        {
            _lockFree.GetDummyObject();
        }

        [Benchmark]
        public void Lock_Parallel()
        {
            Parallel.ForEach(_source, Lock_GetDummyObject);
        }

        private void Lock_GetDummyObject(int _)
        {
            _lock.GetDummyObject();
        }

        private class LockMethod
        {
            private readonly ReadOnlyCollection<object> _dummyObjects;

            private readonly int _maxIndex;
            private readonly object _dummyObjectsLock = new object();
            private int _nextIndex;

            public int Size { get; }

            public LockMethod()
            {
                var dummyObjects = new ReadOnlyCollection<object>(Enumerable.Range(0, 16).Select(x => new object()).ToList());

                _dummyObjects = dummyObjects;
                Size = dummyObjects.Count;
                _maxIndex = Size - 1;
            }

            internal object GetDummyObject()
            {
                int index = 0;
                lock (_dummyObjectsLock)
                {
                    if (_nextIndex > _maxIndex)
                    {
                        _nextIndex = 0;
                    }

                    index = _nextIndex++;
                }

                return _dummyObjects[index];
            }
        }

        private class LockFreeMethod
        {
            private readonly ReadOnlyCollection<object> _dummyObjects;
            private int _nextIndex;

            public int Size { get; }

            public LockFreeMethod()
            {
                var dummyObjects = new ReadOnlyCollection<object>(Enumerable.Range(0, 16).Select(x => new object()).ToList());

                _dummyObjects = dummyObjects;
                Size = dummyObjects.Count;
            }

            internal object GetDummyObject()
            {
                uint index = unchecked((uint)Interlocked.Increment(ref _nextIndex));
                return _dummyObjects[(int)(index % Size)];
            }
        }
    }
}
