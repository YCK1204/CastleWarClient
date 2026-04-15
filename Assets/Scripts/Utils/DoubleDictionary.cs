using System.Collections.Generic;

namespace Utils
{
    public class DoubleDictionary<T1, T2, T3>
    {
        private readonly Dictionary<T1, Dictionary<T2, T3>> _dict = new();

        public bool TryGetValue(T1 k1, T2 k2, out T3 value)
        {
            value = default;
            return _dict.TryGetValue(k1, out var inner) && inner.TryGetValue(k2, out value);
        }

        public bool ContainsKey(T1 k1) => _dict.ContainsKey(k1);

        public bool ContainsKey(T1 k1, T2 k2) =>
            _dict.TryGetValue(k1, out var inner) && inner.ContainsKey(k2);

        public void Set(T1 k1, T2 k2, T3 value)
        {
            if (!_dict.ContainsKey(k1))
                _dict[k1] = new Dictionary<T2, T3>();
            _dict[k1][k2] = value;
        }

        public bool Remove(T1 k1, T2 k2)
        {
            if (!_dict.TryGetValue(k1, out var inner)) return false;
            return inner.Remove(k2);
        }

        public bool Remove(T1 k1) => _dict.Remove(k1);

        public bool TryGetInner(T1 k1, out Dictionary<T2, T3> inner) =>
            _dict.TryGetValue(k1, out inner);

        public IEnumerable<Dictionary<T2, T3>> InnerValues() => _dict.Values;
    }
}
