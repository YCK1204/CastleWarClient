using System.Collections.Generic;
using GameLogic.FieldObjects;
using UnityEngine;

namespace GameLogic
{
    public class FieldObjectRegistry : MonoBehaviour
    {
        public static FieldObjectRegistry Instance { get; private set; }

        private readonly Dictionary<ushort, FieldObject> _objects = new();

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void Register(ushort networkId, FieldObject obj) => _objects[networkId] = obj;
        public void Unregister(ushort networkId) => _objects.Remove(networkId);
        public FieldObject Get(ushort networkId)
        {
            _objects.TryGetValue(networkId, out var obj);
            return obj;
        }
    }
}
