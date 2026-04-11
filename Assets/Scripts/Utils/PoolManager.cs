using System;
using System.Collections.Concurrent;
using CWFramework;

namespace Utils.PoolManager
{
    public interface IPoolable
    {
    }
    
    public abstract class IPool
    {
        protected ConcurrentStack<IPoolable> _pool = new ConcurrentStack<IPoolable>();
        protected int _maxPoolSize = 10;
        protected Func<IPoolable> _factory;
    
        public IPool(Func<IPoolable> factory, int maxPoolSize = 10)
        {
            _factory = factory;
            _maxPoolSize = maxPoolSize;
            for (int i = 0; i < _maxPoolSize; i++)
            {
                _pool.Push(_factory());
            }
        }
    
        public abstract IPoolable Pop();
        public abstract void Push(IPoolable poolable);
    }
    
    public class Pool : IPool
    {
        public Pool(Func<IPoolable> factory, int maxPoolSize = 10) : base(factory, maxPoolSize)
        {
        }
        public override IPoolable Pop()
        {
            if (_pool.TryPop(out var poolable))
                return poolable;
            return _factory.Invoke();
        }
        public override void Push(IPoolable poolable)
        {
            if (_pool.Count < _maxPoolSize)
                _pool.Push(poolable);
        }
    }
    
    public class PoolManager : Singleton<PoolManager>
    {
        private ConcurrentDictionary<string, IPool> _pools = new ConcurrentDictionary<string, IPool>();
    
        public T CreatePool<T>(string name, Func<T> poolFactory) where T : IPool
        {
            T pool = poolFactory();
            return pool;
        }
    
        public void RegisterPool(string name, IPool pool)
        {
            _pools.TryAdd(name, pool);
        }
    
        public T? Pop<T>(string name) where T : class, IPoolable
        {
            if (!_pools.TryGetValue(name, out var pool))
                return null;
            return pool.Pop() as T;
        }
    
        public void Push(string name, IPoolable poolable)
        {
            if (_pools.TryGetValue(name, out var pool))
                pool.Push(poolable);
        }
    }
}

